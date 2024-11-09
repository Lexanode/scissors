using System;
using FluentMigrator;

namespace Core.Migrations.Base;

[Migration(1)]
public class BaseMigration : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\" SCHEMA \"public\"");
    }

    public override void Down()
    {
        throw new NotImplementedException();
    }
}