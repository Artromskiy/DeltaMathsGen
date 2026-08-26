using System;

namespace DeltaMathsGen.Model
{
    internal static class ShaderMetadata
    {
        public static string? CapabilityName(ShaderCapability capability) => capability switch
        {
            ShaderCapability.None => null,
            ShaderCapability.Unknown => throw new InvalidOperationException("Unknown shader capability cannot be serialized."),
            ShaderCapability.Vector => "vector",
            ShaderCapability.Matrix => "matrix",
            ShaderCapability.Quaternion => "quaternion",
            ShaderCapability.Std430 => "std430",
            _ => throw new InvalidOperationException($"Unsupported shader capability value '{capability}'."),
        };

        public static string? ZoneName(ShaderZoneKind zone) => zone switch
        {
            ShaderZoneKind.None => null,
            ShaderZoneKind.Unknown => throw new InvalidOperationException("Unknown shader zone cannot be serialized."),
            ShaderZoneKind.DeltaMaths => "DeltaMaths",
            _ => throw new InvalidOperationException($"Unsupported shader zone value '{zone}'."),
        };

        public static string ModifierToken(ParameterModifier modifier) => modifier switch
        {
            ParameterModifier.None => string.Empty,
            ParameterModifier.Unknown => throw new InvalidOperationException("Unknown parameter modifier cannot be rendered."),
            ParameterModifier.Out => "out",
            ParameterModifier.Ref => "ref",
            _ => throw new InvalidOperationException($"Unsupported parameter modifier value '{modifier}'."),
        };

        public static bool IsKnownCapability(ShaderCapability capability) => capability is
            ShaderCapability.Vector or ShaderCapability.Matrix or ShaderCapability.Quaternion or ShaderCapability.Std430;

        public static bool IsKnownZone(ShaderZoneKind zone) => zone == ShaderZoneKind.DeltaMaths;
    }
}
