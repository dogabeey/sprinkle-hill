using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public static class Busy
    {
        public static bool IsBusy(IBusyChecker checkedUnit)
        {
            return checkedUnit.BusyReasons.Count > 0;
        }
        public static bool IsBusyWith(IBusyChecker checkedUnit, BusyReason reason)
        {
            foreach (var busyReason in checkedUnit.BusyReasons)
            {
                if (busyReason == reason)
                {
                    return true;
                }
            }
            return false;
        }
        public static void SetBusy(IBusyChecker checkedUnit, BusyReason reason)
        {
            if (!checkedUnit.BusyReasons.Contains(reason))
            {
                checkedUnit.BusyReasons.Add(reason);
            }
        }
        public static void ClearBusy(IBusyChecker checkedUnit, BusyReason reason)
        {
            if (checkedUnit.BusyReasons.Contains(reason))
            {
                checkedUnit.BusyReasons.Remove(reason);
            }
        }

    }
    public interface IBusyChecker
    {
        public List<BusyReason> BusyReasons { get; }
    }

    public enum BusyReason
    {
        General,
        IsTransferringElements,
        IsGeneratingGrid,
        IsUpdatingElements
    }
}