using System;
using GameLike.Data.Definitions;
using UnityEngine;

namespace GameLike.Editor.Content
{
    /// <summary>
    /// Converts the GDD's textual weight scale ("---".."+++") into
    /// <see cref="WeightLevel"/>. Centralizing this parsing means every
    /// content importer (Genre, Theme, Console, ...) interprets the scale
    /// identically — no per-importer reinterpretation of "+++".
    /// </summary>
    public static class WeightScaleParser
    {
        /// <summary>
        /// Parses a single scale token. Accepts the exact GDD symbols and an
        /// empty/null string is treated as Neutral by default — callers that
        /// need to distinguish "absent" from "neutral" should check for
        /// null/empty before calling this.
        /// </summary>
        public static WeightLevel Parse(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return WeightLevel.Neutral;

            return token.Trim() switch
            {
                "---" => WeightLevel.StronglyNegative,
                "--" => WeightLevel.Negative,
                "-" => WeightLevel.SlightlyNegative,
                "~" => WeightLevel.Neutral,
                "+" => WeightLevel.SlightlyPositive,
                "++" => WeightLevel.Positive,
                "+++" => WeightLevel.StronglyPositive,
                _ => throw new FormatException(
                    $"\"{token}\" is not a valid GDD weight symbol. Expected one of: --- -- - ~ + ++ +++")
            };
        }

        /// <summary>
        /// Converts a full <see cref="ParameterWeightDto"/> into the runtime
        /// <see cref="ParameterWeightBySize"/> struct, logging which fields
        /// failed to parse instead of throwing, so one bad token in a large
        /// content batch does not abort the entire import.
        /// </summary>
        public static ParameterWeightBySize ParseOrDefault(ParameterWeightDto dto, string contextLabel)
        {
            if (dto == null) return default;

            return new ParameterWeightBySize
            {
                small = TryParse(dto.small, contextLabel, "small"),
                medium = TryParse(dto.medium, contextLabel, "medium"),
                large = TryParse(dto.large, contextLabel, "large"),
                aaa = TryParse(dto.aaa, contextLabel, "aaa")
            };
        }

        private static WeightLevel TryParse(string token, string contextLabel, string sizeLabel)
        {
            try
            {
                return Parse(token);
            }
            catch (FormatException ex)
            {
                Debug.LogWarning($"[WeightScaleParser] {contextLabel} ({sizeLabel}): {ex.Message}. Using Neutral.");
                return WeightLevel.Neutral;
            }
        }
    }
}
