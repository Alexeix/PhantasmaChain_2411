using Phantasma.Domain;
using Phantasma.Numerics;
using Phantasma.Storage;
using Phantasma.Storage.Context;

namespace Phantasma.Contracts.Native
{
    public enum ConstraintKind
    {
        MaxValue,
        MinValue,
        GreatThanOther,
        LessThanOther,
        MustIncrease,
        MustDecrease,
        Deviation,
    }

    public struct ChainConstraint
    {
        public ConstraintKind Kind;
        public BigInteger Value;
        public string Tag;
    }

    public sealed class GovernanceContract : NativeContract
    {
        public override NativeContractKind Kind => NativeContractKind.Governance;

        internal StorageMap _valueMap;
        internal StorageMap _constraintMap;

        public BigInteger FeeMultiplier = 1;

        public GovernanceContract() : base()
        {
        }

        public bool HasName(string name)
        {
            return HasValue(name);
        }

        #region VALUES
        public bool HasValue(string name)
        {
            return _valueMap.ContainsKey<string>(name);
        }

        private void ValidateConstraints(string name, BigInteger previous, BigInteger current, ChainConstraint[] constraints, bool usePrevious)
        {
            for (int i=0; i<constraints.Length; i++)
            {
                var constraint = constraints[i];
                switch (constraint.Kind)
                {
                    case ConstraintKind.MustIncrease:
                        Runtime.Expect(!usePrevious || previous < current, "value must increase");
                        break;

                    case ConstraintKind.MustDecrease:
                        Runtime.Expect(!usePrevious || previous > current, "value must decrease");
                        break;

                    case ConstraintKind.MinValue:
                        Runtime.Expect(current >= constraint.Value, "value is too small");
                        break;

                    case ConstraintKind.MaxValue:
                        Runtime.Expect(current <= constraint.Value, "value is large small");
                        break;

                    case ConstraintKind.GreatThanOther:
                        {
                            Runtime.Expect(name != constraint.Tag, "other tag in constraint must have different name");
                            if (usePrevious)
                            {
                                var other = Runtime.GetGovernanceValue(constraint.Tag);
                                Runtime.Expect(current > other, "value is too small when compared to other");
                            }
                            break;
                        }

                    case ConstraintKind.LessThanOther:
                        {
                            Runtime.Expect(name != constraint.Tag, "other tag in constraint must have different name");
                            if (usePrevious)
                            {
                                var other = Runtime.GetGovernanceValue(constraint.Tag);
                                Runtime.Expect(current < other, "value is too big when compared to other");
                            }
                            break;
                        }

                    case ConstraintKind.Deviation:
                        {
                            Runtime.Expect(false, "deviation constraint not supported yet");
                            break;
                        }               
                 }
            }
        }

        public void CreateValue(string name, BigInteger initial, byte[] serializedConstraints)
        {
            Runtime.Expect(!HasName(name), "name already exists");
            Runtime.Expect(Runtime.IsWitness(Runtime.Nexus.GenesisAddress), "genesis must be witness");

            var constraints = Serialization.Unserialize<ChainConstraint[]>(serializedConstraints);
            ValidateConstraints(name, 0, initial, constraints, false);

            if (name == ValidatorContract.ValidatorCountTag)
            {
                Runtime.Expect(initial == 1, "initial number of validators must always be one");
            }

            _valueMap.Set<string, BigInteger>(name, initial);
            _constraintMap.Set<string, ChainConstraint[]>(name, constraints);

            Runtime.Notify(EventKind.ValueCreate, Runtime.Nexus.GenesisAddress, new ChainValueEventData() { Name = name, Value = initial });
        }

        public BigInteger GetValue(string name)
        {
            Runtime.Expect(HasValue(name), "invalid value name");
            var value = _valueMap.Get<string, BigInteger>(name);
            return value;
        }

        public void SetValue(string name, BigInteger value)
        {
            Runtime.Expect(HasValue(name), "invalid value name");

            var pollName = ConsensusContract.SystemPoll + name;
            var hasConsensus = Runtime.CallContext("consensus", "HasConsensus", pollName, value).AsBool();
            Runtime.Expect(hasConsensus, "consensus not reached");

            var previous = _valueMap.Get<string, BigInteger>(name);
            var constraints = _constraintMap.Get<string, ChainConstraint[]>(name);
            ValidateConstraints(name, previous, value, constraints, true);

            _valueMap.Set<string, BigInteger>(name, value);

            Runtime.Notify(EventKind.ValueUpdate, Runtime.Nexus.GenesisAddress, new ChainValueEventData() { Name = name, Value = value});
        }
        #endregion
    }
}
