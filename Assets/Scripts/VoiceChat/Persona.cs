using System;

namespace EchoTrio {
    /// Personas the actors will role-play.
    public enum Persona {
        Athena,
        Poseidon,
    }

    public static class PersonaExtensions {
        public const Persona DefaultValue = Persona.Athena;

        public static string ToString(this Persona persona) {
            switch (persona) {
                case Persona.Athena: return "Athena";
                case Persona.Poseidon: return "Poseidon";
                default: throw new NotImplementedException();
            }
        }
    }
}