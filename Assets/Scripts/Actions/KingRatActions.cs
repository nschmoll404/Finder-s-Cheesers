using UnityEngine;
using System;

namespace Actions
{
    #region King Rat Actions

    /// <summary>
    /// Drops whatever the KingRatHandler is currently carrying.
    /// </summary>
    [Serializable]
    public class DropKingRatAction : IAction
    {
        [Tooltip("Reference to the KingRatHandler component")]
        public FindersCheesers.KingRatHandler kingRatHandler;

        public void Execute(object context = null)
        {
            if (kingRatHandler != null)
            {
                kingRatHandler.ReleaseKingRat();
            }
            else
            {
                Debug.LogWarning("DropKingRatAction: KingRatHandler is null");
            }
        }
    }

    #endregion
}
