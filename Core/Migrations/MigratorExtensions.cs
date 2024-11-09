using FluentMigrator;
using FluentMigrator.Builders.Create.Table;

namespace Core.Migrations;

public static class MigratorExtensions
{
    public static ICreateTableColumnOptionOrWithColumnSyntax AsPrimaryGuid(this ICreateTableColumnAsTypeSyntax syntax)
    {
        return syntax.AsGuid().PrimaryKey().NotNullable().WithDefault(SystemMethods.NewGuid);
    }
}