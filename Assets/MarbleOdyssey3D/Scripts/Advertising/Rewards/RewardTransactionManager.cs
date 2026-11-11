// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// RewardTransactionManager.cs — Idempotent reward transaction state machine
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Manages reward transactions with idempotent grant semantics.
    /// Prevents duplicate rewards from duplicate SDK callbacks.
    ///
    /// IMPORTANT: Client-side reward validation is NOT equivalent to server-side verification
    /// and cannot fully prevent modified-client fraud. This system provides local idempotency
    /// only. For true fraud prevention, a server-side reward verification service is required.
    /// </summary>
    public class RewardTransactionManager
    {
        private readonly Dictionary<string, RewardTransaction> _transactions =
            new Dictionary<string, RewardTransaction>();
        private readonly HashSet<string> _grantedTransactionIds = new HashSet<string>();

        // Persistence key for PlayerPrefs
        private const string GrantedTxKey = "MarbleOdyssey_GrantedRewardTxIds";
        private const int MaxPersistedTransactions = 500;

        public RewardTransactionManager()
        {
            LoadPersistedTransactions();
        }

        /// <summary>
        /// Begin a new reward transaction. Returns a unique transaction ID.
        /// Call this before showing a rewarded ad.
        /// </summary>
        public string BeginTransaction()
        {
            string txId = GenerateTransactionId();

            var tx = new RewardTransaction
            {
                TransactionId = txId,
                State = RewardTransactionState.Loading,
                CreatedAt = DateTime.UtcNow
            };

            _transactions[txId] = tx;

            Debug.Log($"[RewardTx] Transaction started: {txId}");
            return txId;
        }

        /// <summary>
        /// Check whether a reward can be granted for this transaction.
        /// Returns false if the transaction has already been granted or doesn't exist.
        /// </summary>
        public bool CanGrantReward(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
                return false;

            // Already granted — duplicate prevention
            if (_grantedTransactionIds.Contains(transactionId))
                return false;

            // Must have an active transaction
            if (!_transactions.TryGetValue(transactionId, out var tx))
                return false;

            // Can only grant from certain states
            return tx.State == RewardTransactionState.Loading ||
                   tx.State == RewardTransactionState.Ready ||
                   tx.State == RewardTransactionState.Showing ||
                   tx.State == RewardTransactionState.RewardPending;
        }

        /// <summary>
        /// Grant the reward for a transaction. Marks it as granted and persists.
        /// Idempotent — calling this multiple times for the same transaction
        /// will only grant once.
        /// </summary>
        public bool GrantReward(string transactionId, string rewardType, double rewardAmount)
        {
            if (!CanGrantReward(transactionId))
            {
                Debug.LogWarning($"[RewardTx] Cannot grant reward for tx {transactionId} " +
                                 "(already granted or invalid).");
                return false;
            }

            var tx = _transactions[transactionId];
            tx.State = RewardTransactionState.RewardGranted;
            tx.RewardType = rewardType;
            tx.RewardAmount = rewardAmount;
            tx.GrantedAt = DateTime.UtcNow;
            _transactions[transactionId] = tx;

            _grantedTransactionIds.Add(transactionId);
            PersistGrantedTransactions();

            Debug.Log($"[RewardTx] Reward granted: {rewardAmount} {rewardType} " +
                      $"(tx: {transactionId})");
            return true;
        }

        /// <summary>
        /// Mark a transaction as failed (ad failed to show, user dismissed without reward).
        /// </summary>
        public void FailTransaction(string transactionId)
        {
            if (_transactions.TryGetValue(transactionId, out var tx))
            {
                tx.State = RewardTransactionState.Failed;
                _transactions[transactionId] = tx;
                Debug.Log($"[RewardTx] Transaction failed: {transactionId}");
            }
        }

        /// <summary>
        /// Update the state of a transaction.
        /// </summary>
        public void UpdateState(string transactionId, RewardTransactionState newState)
        {
            if (_transactions.TryGetValue(transactionId, out var tx))
            {
                tx.State = newState;
                _transactions[transactionId] = tx;
            }
        }

        /// <summary>
        /// Get the total number of rewards granted this session.
        /// </summary>
        public int GetSessionRewardCount()
        {
            int count = 0;
            foreach (var tx in _transactions.Values)
            {
                if (tx.State == RewardTransactionState.RewardGranted)
                    count++;
            }
            return count;
        }

        // =====================================================================
        // Persistence
        // =====================================================================

        private void PersistGrantedTransactions()
        {
            try
            {
                // Keep only recent transaction IDs to prevent unbounded growth
                var ids = new List<string>(_grantedTransactionIds);
                if (ids.Count > MaxPersistedTransactions)
                {
                    ids.RemoveRange(0, ids.Count - MaxPersistedTransactions);
                }

                string serialized = string.Join("|", ids);
                PlayerPrefs.SetString(GrantedTxKey, serialized);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RewardTx] Failed to persist transactions: {ex.Message}");
            }
        }

        private void LoadPersistedTransactions()
        {
            try
            {
                string serialized = PlayerPrefs.GetString(GrantedTxKey, "");
                if (!string.IsNullOrEmpty(serialized))
                {
                    string[] ids = serialized.Split('|');
                    foreach (string id in ids)
                    {
                        if (!string.IsNullOrEmpty(id))
                            _grantedTransactionIds.Add(id);
                    }
                    Debug.Log($"[RewardTx] Loaded {_grantedTransactionIds.Count} persisted transaction IDs.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RewardTx] Failed to load persisted transactions: {ex.Message}");
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private string GenerateTransactionId()
        {
            // Generate a locally unique transaction ID
            return $"rwtx_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        // =====================================================================
        // Transaction Data
        // =====================================================================

        public struct RewardTransaction
        {
            public string TransactionId;
            public RewardTransactionState State;
            public DateTime CreatedAt;
            public DateTime GrantedAt;
            public string RewardType;
            public double RewardAmount;
        }
    }
}
