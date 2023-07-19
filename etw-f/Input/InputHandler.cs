using System;
using System.Collections.Generic;

namespace etw_f.Input;

static class InputHandler
{
    internal static InputArguments Parse(string[] args)
    {
        var providers = ParseProviders(args);
        var filters = ParseFilters(args);
        var selectedFields = ParseSelectFields(args);

        return new InputArguments(providers, filters, selectedFields);
    }

    private static string[]? ParseProviders(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-p")
            {
                return args[i + 1].Split(',');
            }
        }

        return null;
    }

    private static IReadOnlyList<(string?, Filtering.FilterExpression)> ParseFilters(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-f" && i + 1 < args.Length)
            {
                var tfilters = new List<(string?, Filtering.FilterExpression)>();
                foreach (string? filter in args[i + 1].Split(','))
                {
                    string?[] filterOptions = filter.Split(':');
                    tfilters.Add((filterOptions[0], new Filtering.PayloadFieldBoundFilterExpression(filterOptions[2], filterOptions[1])));
                }

                return tfilters;
            }
        }

        return Array.Empty<(string?, Filtering.FilterExpression)>();
    }

    private static IReadOnlyList<(string, IReadOnlyList<string>)> ParseSelectFields(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-s" && i + 1 < args.Length)
            {
                var tSelectedFields = new List<(string?, IReadOnlyList<string>?)>();
                foreach (string? filter in args[i + 1].Split('|'))
                {
                    string?[] filterOptions = filter.Split(':');
                    tSelectedFields.Add((filterOptions[0], filterOptions[1].Split(",")));
                }

                return tSelectedFields;
            }
        }

        return Array.Empty<(string, IReadOnlyList<string>)>();
    }

    internal static void ThrowArgumentError()
    {
        throw new ArgumentException("Incorrect input argument format is: provider filters selectedfields");
    }
}
