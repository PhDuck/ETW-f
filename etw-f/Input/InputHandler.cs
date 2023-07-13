using System.Collections.Generic;

namespace etw_f.Input;

static class InputHandler
{
    internal static InputArguments Parse(string[] args)
    {
        string[] providers = args[0].Split(',');

        var filters = new List<(string?, Filtering.FilterExpression)>();
        if (args.Length > 1)
        {
            foreach (string? filter in args[1].Split(','))
            {
                string?[] filterOptions = filter.Split(':');
                filters.Add((filterOptions[0], new Filtering.PayloadFieldBoundFilterExpression(filterOptions[2], filterOptions[1])));
            }
        }

        var selectedFields = new List<(string, IReadOnlyList<string>)>();
        if (args.Length > 2)
        {
            foreach (string? filter in args[2].Split('|'))
            {
                string?[] filterOptions = filter.Split(':');
                selectedFields.Add((filterOptions[0], filterOptions[1].Split(",")));
            }
        }

        return new InputArguments(providers, filters, selectedFields);
    }
}