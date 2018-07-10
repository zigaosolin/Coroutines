using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Coroutines.Implementation
{
    struct TimerKey : IComparable<TimerKey>
    {
        public long TimeInTicks;
        public int SecondarySort;

        public TimerKey(long timeInTicks, int secondaryID)
        {
            TimeInTicks = timeInTicks;
            SecondarySort = secondaryID;
        }

        public int CompareTo(TimerKey other)
        {
            int result = TimeInTicks.CompareTo(other.TimeInTicks);
            if (result != 0)
                return result;

            return SecondarySort.CompareTo(other.SecondarySort);
        }
    }

    internal class TimerTrigger<TCoroutineContinuationData>
    {
        long currentTime = 0;
        int currentSecondaryID;
        SortedDictionary<TimerKey, TCoroutineContinuationData> triggers = new SortedDictionary<TimerKey, TCoroutineContinuationData>();

        public void Update(float deltaTime, Action<TCoroutineContinuationData> triggerAction)
        {
            currentTime += (long)(deltaTime * 1000);

            List<TimerKey> toRemoveList = new List<TimerKey>();
            foreach(var trigger in triggers)
            {
                if(trigger.Key.TimeInTicks <= currentTime)
                {
                    triggerAction(trigger.Value);
                    toRemoveList.Add(trigger.Key);
                    continue;
                }
                break;
            }

            foreach(var toRemove in toRemoveList)
            {
                triggers.Remove(toRemove);
            }
        }

        public void AddTrigger(float waitForSeconds, TCoroutineContinuationData data)
        {
            long timeStamp = currentTime + (long)(waitForSeconds * 1000);
            triggers.Add(new TimerKey(timeStamp, currentSecondaryID++), data);
        }
    }
}
