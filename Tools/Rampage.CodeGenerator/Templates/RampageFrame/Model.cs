using Rampage.Data;
using System;

namespace @{Namespace}.Model.Po
{
    /// <summary>
    /// @{TableComment}
    /// </summary>
    [TableMapping("@{TableName}")]
    public class @{EntityName}
    {
        {@each ColumnList as column, index}
        /// <summary>
        /// @{column.Comment}
        /// </summary>
        [FieldMapping({@if(column.PrimaryKey)}PrimaryKey = true, {/if}{@if (column.AutoIncrement)}AutoIncrement = true, {/if}ColumnName = "@{column.ColumnName}")]
        public @{column.LanguageType}{@if(column.IsNullable)}?{/if} @{column.PropertyName} { get; set; }
        {/each}
    }
}
