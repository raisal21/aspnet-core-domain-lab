using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetCoreDomainLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "healthcare");
            migrationBuilder.EnsureSchema(name: "industrial");
            migrationBuilder.EnsureSchema(name: "logistics");
            migrationBuilder.EnsureSchema(name: "banking");
            migrationBuilder.EnsureSchema(name: "auth");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSchema(name: "auth");
            migrationBuilder.DropSchema(name: "banking");
            migrationBuilder.DropSchema(name: "logistics");
            migrationBuilder.DropSchema(name: "industrial");
            migrationBuilder.DropSchema(name: "healthcare");
        }
    }
}
