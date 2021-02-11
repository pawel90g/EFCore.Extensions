﻿using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Garbacik.NetCore.EFCore.Extensions
{
    public static class DbContextExtensions
    {
        public static async Task BulkInsertAsync<T>(this DbContext context, IEnumerable<T> collection)
        {
            var dataTable = MapToDataTable(context, collection);
            using (var connection = new SqlConnection(context.Database.GetDbConnection().ConnectionString))
            {
                connection.Open();
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = context.Model.FindEntityType(typeof(T)).GetTableName();
                    bulkCopy.ColumnMappings.Clear();
                    await bulkCopy.WriteToServerAsync(dataTable);
                }
                connection.Close();
            }
        }
        private static DataTable MapToDataTable<T>(DbContext context, IEnumerable<T> collection)
        {
            var dataTable = new DataTable();

            var primaryKeys = context.Model
                .FindEntityType(typeof(T))
                .FindPrimaryKey()
                .Properties
                .Select(x => new DataColumn(x.Name, x.ClrType))
                .ToArray();

            dataTable.Columns.AddRange(primaryKeys.ToArray());

            typeof(T).GetProperties().ToList().ForEach(p =>
            {
                if (!dataTable.Columns.Contains(p.Name))
                    dataTable.Columns.Add(p.Name, p.PropertyType);
            });
            dataTable.PrimaryKey = primaryKeys;

            foreach (var item in collection)
                dataTable.Rows.Add(MapToDataRow(dataTable, item));

            return dataTable;
        }

        private static DataRow MapToDataRow<T>(DataTable dataTable, T entity)
        {
            var row = dataTable.NewRow();

            entity.GetType().GetProperties().ToList().ForEach(p =>
            {
                row[p.Name] = p.GetValue(entity);
            });

            return row;
        }
    }
}
