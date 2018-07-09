using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Coroutines.Implementation
{
    struct SmartTimerKey : IComparable<SmartTimerKey>
    {
        public long TimeInTicks;
        public int SecondarySort;

        public SmartTimerKey(long timeInTicks, int secondaryID)
        {
            this.TimeInTicks = timeInTicks;
            this.SecondarySort = secondaryID;
        }

        public int CompareTo(SmartTimerKey other)
        {
            int result = TimeInTicks.CompareTo(other.TimeInTicks);
            if (result != 0)
                return result;

            return SecondarySort.CompareTo(other.SecondarySort);
        }
    }

    internal class SmartTimerTrigger
    {
        long currentTime = 0;
        int currentSecondaryID;
        SortedDictionary<SmartTimerKey, CoroutineState> triggers = new SortedDictionary<SmartTimerKey, CoroutineState>();

        public void Update(float deltaTime, ConcurrentQueue<CoroutineState> trigerredCoroutines)
        {
            currentTime += (long)(deltaTime * 1000);

            List<SmartTimerKey> toRemoveList = new List<SmartTimerKey>();
            foreach(var trigger in triggers)
            {
                if(trigger.Key.TimeInTicks <= currentTime)
                {
                    trigerredCoroutines.Enqueue(trigger.Value);
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

        public void AddTrigger(float waitForSeconds, CoroutineState coroutine)
        {
            long timeStamp = currentTime + (long)(waitForSeconds * 1000);
            triggers.Add(new SmartTimerKey(timeStamp, currentSecondaryID++), coroutine);
        }
    }
}
