using System;
using System.Collections.Generic;
using WMG.Core;
using WMG.Gestures;

namespace WMG.Reactions
{
    /*
     * Represents a possible reaction to a mouse gesture, such as "maximize the active window".
     */
    public abstract class Reaction
    {
        /* Do not allow for additional child classes. */
        internal Reaction() { }

        public abstract ReactionType RType { get; }

        public abstract void Perform(Gesture gesture, IContext context);

        public static Reaction FromString(string str, ISerializationContext context)
        {
            foreach (ReactionType t in ReactionType.KNOWN_TYPES)
            {
                var result = t.LoadString(str, context);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }

    public interface IContext
    {
        /*
         * Exits the entire application.
         */
        void ExitApplication();

        /*
         * Usage:
         *      using (context.DisableTemporarily())
         *      {
         *          // do stuff while gesture recognition is disabled
         *      }
         *      // gesture recognition is enabled again
         */
        IDisposable DisableTemporarily();
    }

    /*
     * Responsible for saving / loading Reactions of the associated type.
     * Future plan: also describes the available settings for the type of reaction in such a way that a UI can be generated.
     * Way future plan: load reactions and reaction types from plugin dlls.
     */
    public abstract class ReactionType
    {
        internal ReactionType() { }

        /* 
         * The string should include an identifier for the type of reaction (i.e., for the child class).
         * Returns null if the given Reaction is not of the right type.
         */
        public abstract string StoreString(Reaction r, ISerializationContext context);

        /*
         * Reconstructs the Reaction from the given string.
         * Returns null if the string is not of the right type, i.e., does not contain the correct identifier.
         */
        public abstract Reaction LoadString(string str, ISerializationContext context);

        internal static readonly List<ReactionType> KNOWN_TYPES = new List<ReactionType>
        {
            ExitType.INSTANCE,
            CloseWindowType.INSTANCE,
            MinimizeWindowType.INSTANCE,
            MaximizeWindowType.INSTANCE,
            MoveWindowType.INSTANCE
        };
    }

    public interface ISerializationContext
    {
        int AddObjectReference(object obj);
        // TODO. Idea will be: if object is not known yet, generate ID for it and return it
        // If object is already known, just return the ID
        // After going through all the reactions and invoking storestring on each, we will then serialize the stored objects with their IDs

        object RetrieveObject(int id);
    }
}
