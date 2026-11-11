// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// RewardIdempotencyTests.cs — Unit tests for reward transaction idempotency
// ============================================================================

using NUnit.Framework;

namespace MarbleOdyssey.Advertising.Tests
{
    [TestFixture]
    public class RewardIdempotencyTests
    {
        private RewardTransactionManager _txManager;

        [SetUp]
        public void SetUp()
        {
            _txManager = new RewardTransactionManager();
        }

        [Test]
        public void BeginTransaction_ReturnsUniqueId()
        {
            string tx1 = _txManager.BeginTransaction();
            string tx2 = _txManager.BeginTransaction();

            Assert.IsNotNull(tx1);
            Assert.IsNotNull(tx2);
            Assert.AreNotEqual(tx1, tx2, "Transaction IDs should be unique.");
        }

        [Test]
        public void CanGrantReward_ForNewTransaction_ReturnsTrue()
        {
            string txId = _txManager.BeginTransaction();

            bool canGrant = _txManager.CanGrantReward(txId);
            Assert.IsTrue(canGrant, "Should allow granting reward for new transaction.");
        }

        [Test]
        public void CanGrantReward_ForNullId_ReturnsFalse()
        {
            bool canGrant = _txManager.CanGrantReward(null);
            Assert.IsFalse(canGrant);
        }

        [Test]
        public void CanGrantReward_ForEmptyId_ReturnsFalse()
        {
            bool canGrant = _txManager.CanGrantReward("");
            Assert.IsFalse(canGrant);
        }

        [Test]
        public void CanGrantReward_ForUnknownId_ReturnsFalse()
        {
            bool canGrant = _txManager.CanGrantReward("unknown_tx_id");
            Assert.IsFalse(canGrant);
        }

        [Test]
        public void GrantReward_MarksTransactionComplete()
        {
            string txId = _txManager.BeginTransaction();

            bool granted = _txManager.GrantReward(txId, "gems", 50);
            Assert.IsTrue(granted, "First grant should succeed.");

            bool canGrantAgain = _txManager.CanGrantReward(txId);
            Assert.IsFalse(canGrantAgain, "Should not allow re-granting.");
        }

        [Test]
        public void GrantReward_DuplicateCallReturnsFalse()
        {
            string txId = _txManager.BeginTransaction();

            bool first = _txManager.GrantReward(txId, "gems", 50);
            bool second = _txManager.GrantReward(txId, "gems", 50);

            Assert.IsTrue(first, "First grant should succeed.");
            Assert.IsFalse(second, "Duplicate grant should fail.");
        }

        [Test]
        public void GrantReward_DifferentTransactionsSucceed()
        {
            string tx1 = _txManager.BeginTransaction();
            string tx2 = _txManager.BeginTransaction();

            bool granted1 = _txManager.GrantReward(tx1, "gems", 50);
            bool granted2 = _txManager.GrantReward(tx2, "coins", 25);

            Assert.IsTrue(granted1);
            Assert.IsTrue(granted2);
        }

        [Test]
        public void FailTransaction_PreventsGrant()
        {
            string txId = _txManager.BeginTransaction();

            _txManager.FailTransaction(txId);

            bool canGrant = _txManager.CanGrantReward(txId);
            Assert.IsFalse(canGrant, "Failed transactions should not be grantable.");
        }

        [Test]
        public void GetSessionRewardCount_TracksGrantedRewards()
        {
            string tx1 = _txManager.BeginTransaction();
            string tx2 = _txManager.BeginTransaction();
            string tx3 = _txManager.BeginTransaction();

            _txManager.GrantReward(tx1, "gems", 50);
            _txManager.GrantReward(tx2, "coins", 25);
            _txManager.FailTransaction(tx3);

            int count = _txManager.GetSessionRewardCount();
            Assert.AreEqual(2, count, "Should count only granted rewards.");
        }

        [Test]
        public void TransactionId_HasExpectedFormat()
        {
            string txId = _txManager.BeginTransaction();

            Assert.IsTrue(txId.StartsWith("rwtx_"),
                "Transaction ID should start with 'rwtx_' prefix.");
        }

        [Test]
        public void MultipleRapidTransactions_AllUnique()
        {
            var ids = new System.Collections.Generic.HashSet<string>();

            for (int i = 0; i < 100; i++)
            {
                string txId = _txManager.BeginTransaction();
                Assert.IsTrue(ids.Add(txId),
                    $"Transaction ID {txId} was duplicated on iteration {i}.");
            }
        }
    }
}
