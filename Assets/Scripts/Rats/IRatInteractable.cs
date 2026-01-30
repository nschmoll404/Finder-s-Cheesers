using System.Collections.Generic;
using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Interface for objects that can interact with rats by spending or depositing them.
    /// Implementations should handle the logic of what happens when a rat is spent/deposited,
    /// and the RatInventory will lose the rat as a result.
    /// </summary>
    public interface IRatInteractable
    {
        /// <summary>
        /// Gets the number of rats required to interact with this interactable.
        /// </summary>
        int RatCost { get; }

        /// <summary>
        /// Gets the number of rats currently deposited in this interactable.
        /// </summary>
        int DepositedRatsCount { get; }

        /// <summary>
        /// Gets whether rats can be withdrawn from this interactable.
        /// </summary>
        bool AllowWithdrawal { get; }

        /// <summary>
        /// Gets a description of what happens when a rat is spent/deposited.
        /// This can be used for UI or tooltips.
        /// </summary>
        string InteractionDescription { get; }

        /// <summary>
        /// Gets the transform of this interactable for positioning or distance calculations.
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Checks if this interactable can accept rats (deposit).
        /// </summary>
        /// <returns>True if rats can be deposited, false otherwise.</returns>
        bool CanDepositRats();

        /// <summary>
        /// Checks if rats can be withdrawn from this interactable.
        /// </summary>
        /// <returns>True if rats can be withdrawn, false otherwise.</returns>
        bool CanWithdrawRats();

        /// <summary>
        /// Deposits rats to this interactable.
        /// </summary>
        /// <param name="rats">The list of rats to deposit.</param>
        /// <returns>True if all rats were successfully deposited, false otherwise.</returns>
        bool DepositRats(List<Rat> rats);

        /// <summary>
        /// Withdraws rats from this interactable.
        /// </summary>
        /// <param name="ratInventory">The RatInventory to return rats to.</param>
        /// <returns>The list of withdrawn rats, or null if withdrawal failed.</returns>
        List<Rat> WithdrawRats(RatInventory ratInventory);
    }
}
