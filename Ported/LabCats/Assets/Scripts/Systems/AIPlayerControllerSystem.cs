using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.Rendering;

public class AIPlayerControllerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireSingletonForUpdate<BoardInitializedTag>();
    }
    
    protected override void OnUpdate()
    {
        var boardEntity = GetSingletonEntity<BoardInitializedTag>();
        var boardDefinition = GetSingleton<BoardDefinition>();
        
        const float cursorSpeed = 3.0f;
        
        var firstCellPosition = EntityManager.GetComponentData<FirstCellPosition>(boardEntity);
        var timeData = this.Time;
        
        var random = new Random(1234);
        
        int numberOfRows = boardDefinition.NumberRows;
        int numberOfColumns = boardDefinition.NumberColumns;
        
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        Entities.WithName("ComputeMovementForCursor").ForEach((Entity e, ref AITargetCell aiTargetCell, ref DynamicBuffer<ArrowReference> arrows, ref Translation translation) =>
        {
            DynamicBuffer<GridCellContent> gridCellContents = GetBufferFromEntity<GridCellContent>()[boardEntity];
            var cellOffSet = new float3(boardDefinition.CellSize * aiTargetCell.X, 1.0f, boardDefinition.CellSize * aiTargetCell.Y);
            float3 targetCellPosition = firstCellPosition.Value + cellOffSet;
         
            var distanceVector = targetCellPosition - translation.Value;
            var movementDirection = math.normalize(distanceVector);
            var squareDistance = distanceVector.x * distanceVector.x + distanceVector.y * distanceVector.y + distanceVector.z * distanceVector.z;
            var distance = math.sqrt(squareDistance);
        
            if (cursorSpeed * timeData.DeltaTime > distance)
            {
                // the cursor has reached its target point, we need to change the cell to have an arrow and setup a new targetCell
                int selectedArrowIndex = -1;
                Entity selectedArrow;
                for (int i = 0; i < arrows.Length; i++)
                {
                    if (arrows[i].Entity == Entity.Null)
                    {
                        selectedArrowIndex = i;
                        break;
                    }
                }
                
                if (selectedArrowIndex == -1)
                    selectedArrowIndex = 0;
                selectedArrow = arrows[selectedArrowIndex].Entity;
                
                // Move with arrow
                {
                    var index = GridCellContent.Get1DIndexFromGridPosition(aiTargetCell.X, aiTargetCell.Y, numberOfColumns);
                    if (selectedArrow != Entity.Null)
                    {
                        var oldArrowPosition = GetComponent<GridPosition>(selectedArrow);
                        var oldGridContentValue = gridCellContents[GridCellContent.Get1DIndexFromGridPosition(oldArrowPosition.X, oldArrowPosition.Y, numberOfColumns)];
                        oldGridContentValue.Type = GridCellType.None;
                        gridCellContents[index] = oldGridContentValue;
                    }
                
                    var gridContent = gridCellContents[index];
                    gridContent.Type = GridCellType.ArrowLeft; //Why left, I don’t know
                    gridCellContents[index] = gridContent;
                
                    ecb.SetComponent(selectedArrow, new GridPosition(){X = aiTargetCell.X, Y = aiTargetCell.Y});
                
                }
                //Compute new target

                var newTargetX = random.NextInt(0, numberOfRows);
                var newTargetY = random.NextInt(0, numberOfColumns);
    
                aiTargetCell = new AITargetCell(){X = newTargetX, Y = newTargetY};
            }
            var progress = movementDirection * math.min(cursorSpeed * timeData.DeltaTime, distance);
        
            translation.Value = translation.Value + progress;
        
        
        }).Run();
        //
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}