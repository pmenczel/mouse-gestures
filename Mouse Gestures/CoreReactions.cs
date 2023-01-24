using WMG.Gestures;

namespace WMG.Reactions
{
    public sealed class ExitReaction : Reaction
    {
        public override ReactionType RType => ExitType.INSTANCE;

        public override void Perform(Gesture gesture, IContext context)
        {
            context.ExitApplication();
        }
    }

    public sealed class ExitType : ReactionType
    {
        private ExitType() { }

        private static readonly string IDENTIFIER = "Exit";

        public static readonly ExitType INSTANCE = new();

        public override string? StoreString(Reaction r, ISerializationContext context)
        {
            if (r is ExitReaction)
            {
                return IDENTIFIER;
            }
            return null;
        }

        public override Reaction? LoadString(string str, ISerializationContext context)
        {
            if (str == IDENTIFIER)
            {
                return new ExitReaction();
            }
            return null;
        }
    }
}
