using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

public static class Db {
    public const int NoCommandTimeout = 0;

    public const int MaxInClauseParameters = 2000;

    public static List<T> Query<T>(SqlConnection cn, SqlTransaction tx, string sql, Func<SqlDataReader, T> map,
                                   params (string Name, object Value)[] parameters) {
        using var cmd = new SqlCommand(sql, cn, tx) { CommandTimeout = NoCommandTimeout };

        foreach (var parameter in parameters) {
            AddParameter(cmd, parameter.Name, parameter.Value);
        }

        var results = new List<T>();

        using var reader = cmd.ExecuteReader();

        while (reader.Read()) {
            results.Add(map(reader));
        }

        return results;
    }

    public static int Execute(SqlConnection cn, SqlTransaction tx, string sql,
                              params (string Name, object Value)[] parameters) {
        using var cmd = new SqlCommand(sql, cn, tx) { CommandTimeout = NoCommandTimeout };

        foreach (var parameter in parameters) {
            AddParameter(cmd, parameter.Name, parameter.Value);
        }

        return cmd.ExecuteNonQuery();
    }

    public static int ExecuteIdentity(SqlConnection cn, SqlTransaction tx, string sql,
                                      params (string Name, object Value)[] parameters) {
        using var cmd = new SqlCommand(sql, cn, tx) { CommandTimeout = NoCommandTimeout };

        foreach (var parameter in parameters) {
            AddParameter(cmd, parameter.Name, parameter.Value);
        }

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public static T Scalar<T>(SqlConnection cn, SqlTransaction tx, string sql,
                              params (string Name, object Value)[] parameters) {
        using var cmd = new SqlCommand(sql, cn, tx) { CommandTimeout = NoCommandTimeout };

        foreach (var parameter in parameters) {
            AddParameter(cmd, parameter.Name, parameter.Value);
        }

        var result = cmd.ExecuteScalar();

        return result == null || result == DBNull.Value ? default : (T) Convert.ChangeType(result, typeof(T));
    }

    public static bool ColumnExists(SqlConnection cn, SqlTransaction tx, string table, string column) {
        return Scalar<int>(cn, tx,
                           "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t AND COLUMN_NAME = @c",
                           ("@t", table), ("@c", column)) > 0;
    }

    private static void AddParameter(SqlCommand cmd, string name, object value) {
        var parameter = new SqlParameter(name, ToSqlDbType(value));

        if (value is string) {
            parameter.Size = -1;
        }

        parameter.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(parameter);
    }

    private static SqlDbType ToSqlDbType(object value) {
        return value switch {
            int => SqlDbType.Int,
            long => SqlDbType.BigInt,
            bool => SqlDbType.Bit,
            Guid => SqlDbType.UniqueIdentifier,
            DateTime => SqlDbType.DateTime2,
            _ => SqlDbType.NVarChar
        };
    }

    public static List<T> QueryIn<T>(SqlConnection cn, SqlTransaction tx, string sqlFormat, string prefix,
                                     IEnumerable<object> values, Func<SqlDataReader, T> map,
                                     params (string Name, object Value)[] extraParameters) {
        var results = new List<T>();

        foreach (var batch in Batch(values)) {
            var (clause, parameters) = BuildInClause(prefix, batch);

            results.AddRange(Query(cn,
                                   tx,
                                   string.Format(sqlFormat, clause),
                                   map,
                                   parameters.Concat(extraParameters).ToArray()));
        }

        return results;
    }

    private static IEnumerable<List<object>> Batch(IEnumerable<object> values) {
        var batch = new List<object>(MaxInClauseParameters);

        foreach (var value in values) {
            batch.Add(value);

            if (batch.Count == MaxInClauseParameters) {
                yield return batch;

                batch = new List<object>(MaxInClauseParameters);
            }
        }

        if (batch.Count > 0) {
            yield return batch;
        }
    }

    public static (string Clause, (string, object)[] Parameters) BuildInClause(string prefix, IEnumerable<object> values) {
        var list = values.ToList();

        if (list.Count > MaxInClauseParameters) {
            throw new InvalidOperationException(
                $"Cannot build an IN clause with {list.Count} parameter(s) — SQL Server caps a command at 2100. " +
                $"Use QueryIn, which batches.");
        }

        var names = new string[list.Count];
        var parameters = new (string, object)[list.Count];

        for (var i = 0; i < list.Count; i++) {
            var name = $"@{prefix}{i}";

            names[i] = name;
            parameters[i] = (name, list[i]);
        }

        return (string.Join(",", names), parameters);
    }
}
