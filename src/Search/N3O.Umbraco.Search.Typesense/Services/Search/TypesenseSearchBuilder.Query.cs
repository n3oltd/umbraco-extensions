using N3O.Umbraco.Extensions;
using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace N3O.Umbraco.Search.Typesense;

public partial class TypesenseSearchBuilder<T> {
    private string _queryFields;
    private string _queryText = "*";

    public ITypesenseSearchBuilder<T> Query<TField>(string text,
                                                    params Expression<Func<T, TField>>[] fieldSelectors) {
        var fields = fieldSelectors.Select(TypesenseField.Get).ToList();

        _queryText = text;
        _queryFields = fields.ToCsv();

        return this;
    }

    public ITypesenseSearchBuilder<T> Query(string text,
                                            params (Expression<Func<T, object>> field, int weight)[] weightedFields) {
        var fields = weightedFields.Select(x => TypesenseField.Get(x.field)).ToList();
        var weights = weightedFields.Select(x => x.weight.ToString(CultureInfo.InvariantCulture)).ToList();

        _queryText = text;
        _queryFields = fields.ToCsv();

        return Custom(p => p.QueryByWeights = weights.ToCsv());
    }
}
