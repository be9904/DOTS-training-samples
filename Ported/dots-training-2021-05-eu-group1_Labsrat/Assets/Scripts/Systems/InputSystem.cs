using System;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = Unity.Mathematics.Random;
using UnityCamera = UnityEngine.Camera;
using UnityGameObject = UnityEngine.GameObject;
using UnityInput = UnityEngine.Input;
using UnityKeyCode = UnityEngine.KeyCode;
using UnityMeshRenderer = UnityEngine.MeshRenderer;
using UnityMonoBehaviour = UnityEngine.MonoBehaviour;
using UnityRangeAttribute = UnityEngine.RangeAttribute;

[UpdateInGroup(typeof(ChuChuRocketUpdateGroup))]
[UpdateAfter(typeof(BoardSpawner))]
public class InputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var gameConfig = GetSingleton<GameConfig>();
        
        var localToWorldData = GetComponentDataFromEntity<LocalToWorld>();
        var cellArray = World.GetExistingSystem<BoardSpawner>().cells;      
        var mousePos = UnityCamera.main.ScreenPointToRay(Input.mousePosition);
        var mouseScreenPos = new float2 (Input.mousePosition.x/Screen.width,Input.mousePosition.y/Screen.height);
        var mouseDown = Input.GetMouseButtonDown(0);
        var mousewheel = Input.mouseScrollDelta.y;
        bool isMouseOverViewport = UnityCamera.main.rect.Contains(UnityCamera.main.ScreenToViewportPoint(Input.mousePosition));
        
        var time = Time.DeltaTime;
        var random = Random.CreateFromIndex((uint)System.DateTime.Now.Ticks);

        Entities
            .WithoutBurst() // We are using UnityEngine.Plane which is not supported by Burst
            .WithNone<AIState>()
            .ForEach((ref PlayerInput playerInput, in PlayerIndex playerIndex) =>
            {
                Assert.IsTrue(playerIndex.Index == 0);
                playerInput.TileIndex = RaycastCellDirection(mousePos, gameConfig, localToWorldData, cellArray, out playerInput.ArrowDirection);
                playerInput.IsMouseDown = mouseDown;

                if (!gameConfig.FixedCamera)
                {
                    if (isMouseOverViewport)
                    {
                        var camera = this.GetSingleton<GameObjectRefs>().Camera;
                        var mouseAxis = mouseScreenPos;
                        mouseAxis = new float2(mouseAxis.x - 0.5f, mouseAxis.y - 0.5f);
                        mouseAxis *= 2;
                        var mouseCenter = math.max(0, math.abs(mouseAxis) - 0.5f) * 2;
                        mouseAxis = math.clamp(mouseAxis * mouseCenter, -0.3f, 0.3f);



                        float3 cameraPos = camera.transform.position;

                        float scrollY = -mousewheel * 3f;
                        cameraPos.y = math.max(cameraPos.y + scrollY, 5f);
                        cameraPos.x += mouseAxis.x * gameConfig.ControlSensitivity * cameraPos.y;
                        cameraPos.z += mouseAxis.y * gameConfig.ControlSensitivity * cameraPos.y;

                        camera.transform.position = cameraPos;
                    }
                }
            }).Run();
        
        Entities
            .ForEach((ref PlayerInput playerInput, ref AIState aiState, ref Translation translation, in PlayerIndex playerIndex) =>
            {
                float ThinkingDelay = 2.0f;
                float CursorSpeed = 2.0f;

                playerInput.IsMouseDown = false;
                
                if (aiState.state == AIState.State.Thinking && aiState.SecondsSinceClicked > ThinkingDelay)
                {
                    // pick random cell on the board
                    int cellX = random.NextInt(gameConfig.BoardDimensions.x);
                    int cellY = random.NextInt(gameConfig.BoardDimensions.y);
                    float3 targetPos = Utils.CellCoordinatesToWorldPosition(cellX, cellY);
                    targetPos.y = 0.1f;
                    aiState.TargetPosition = targetPos;

                    aiState.state = AIState.State.MovingToTarget;
                }

                if (aiState.state == AIState.State.MovingToTarget)
                {
                    translation.Value = math.lerp(translation.Value, aiState.TargetPosition, time * CursorSpeed);

                    if (math.distance(translation.Value, aiState.TargetPosition) < 0.01f)
                    {
                        // click!
                        playerInput.IsMouseDown = true;
                        playerInput.TileIndex = Utils.WorldPositionToCellIndex(translation.Value, gameConfig);
                        playerInput.ArrowDirection = Direction.FromRandomDirection(random.NextInt(4));
                        
                        aiState.SecondsSinceClicked = 0;
                        aiState.state = AIState.State.Thinking;
                    }
                }

                aiState.SecondsSinceClicked += time;

            }).Schedule();
        
    }
    
    public static int RaycastCellDirection(Ray ray, GameConfig gameConfig, ComponentDataFromEntity<LocalToWorld> localToWorldData, NativeArray<Entity> cells, out Cardinals cellDirection)
    {
        cellDirection = Cardinals.North;

        float enter;
        var plane = new Plane(Vector3.up, new Vector3(0, 0.0f, 0));

        if (!plane.Raycast(ray, out enter))
            return -1;

        var worldPos = ray.GetPoint(enter);
        var cell = Utils.WorldPositionToCellIndex(worldPos, gameConfig);

        if (cell < 0)
            return cell;

        Entity cellEntity = cells[cell];
        LocalToWorld cellLocalToWorldTransform = localToWorldData[cellEntity];

        float4x4 worldToLocal = math.inverse(cellLocalToWorldTransform.Value);
        var pt = math.transform(worldToLocal, new float3(worldPos));
        //var pt = worldPos;//cell.transform.InverseTransformPoint(worldPos);
        
        if (Mathf.Abs(pt.z) > Mathf.Abs(pt.x))
            cellDirection = pt.z > 0 ? Cardinals.North : Cardinals.South;
        else
            cellDirection = pt.x > 0 ? Cardinals.East : Cardinals.West;

        return cell;

    }

    public static float3 RayCastPlayerPos(Ray ray)
    {
        float3 PlayerPos = new float3(0,0,0);
        return PlayerPos;
    }

}