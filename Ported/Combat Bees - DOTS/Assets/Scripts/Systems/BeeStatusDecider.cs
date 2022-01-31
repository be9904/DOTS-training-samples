using Unity.Entities;
using UnityEngine;

public partial class BeeStatusDecider : SystemBase
{
    //maybe put this script in initialisation system group !? 
    protected override void OnUpdate()
    {
        Entities.WithAll<BeeTag>().ForEach((ref RandomState randomState, ref Agression agression, ref BeeStatus beeStatus, in BeeDead beeDead) => {
            if (beeStatus.Value == Status.Idle && !beeDead.Value)
            {
                if (randomState.Value.NextFloat()-0.1 < agression.Value)
                {
                    beeStatus.Value = Status.Gathering; 
                }
                else
                {
                    beeStatus.Value = Status.Attacking;
                }
            }
        }).ScheduleParallel();
    }
}
