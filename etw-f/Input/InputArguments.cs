using System.Collections.Generic;
using etw_f.Filtering;

namespace etw_f.Input;

internal readonly struct InputArguments
{
    internal readonly IReadOnlyList<string> Providers;
    internal readonly IReadOnlyList<(string eventName, FilterExpression)> Filters;
    internal readonly IReadOnlyList<(string eventName, IReadOnlyList<string> fieldNames)> SelectedFields;

    public InputArguments(IReadOnlyList<string> providers, IReadOnlyList<(string, FilterExpression)> filters, IReadOnlyList<(string, IReadOnlyList<string>)> selectedField)
    {
        Providers = providers;
        Filters = filters;
        SelectedFields = selectedField;
    }
}
