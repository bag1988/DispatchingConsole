using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DispatchingConsole.Server.Migrations
{
    /// <inheritdoc />
    public partial class changeBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NameCu",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StaffId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "ContactForUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StaffId = table.Column<int>(type: "INTEGER", nullable: false),
                    NameCu = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorityUrl = table.Column<string>(type: "TEXT", nullable: true),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    LastActive = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UserIdentityId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactForUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactForUser_Users_UserIdentityId",
                        column: x => x.UserIdentityId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NoReadMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserIdentityId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChatInfoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoReadMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoReadMessages_Chats_ChatInfoId",
                        column: x => x.ChatInfoId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NoReadMessages_Users_UserIdentityId",
                        column: x => x.UserIdentityId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SharedContact",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StaffId = table.Column<int>(type: "INTEGER", nullable: false),
                    NameCu = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorityUrl = table.Column<string>(type: "TEXT", nullable: true),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    LastActive = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedContact", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_UserIdentityId",
                table: "Chats",
                column: "UserIdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactForUser_UserIdentityId",
                table: "ContactForUser",
                column: "UserIdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_NoReadMessages_ChatInfoId",
                table: "NoReadMessages",
                column: "ChatInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_NoReadMessages_UserIdentityId",
                table: "NoReadMessages",
                column: "UserIdentityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Users_UserIdentityId",
                table: "Chats",
                column: "UserIdentityId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Users_UserIdentityId",
                table: "Chats");

            migrationBuilder.DropTable(
                name: "ContactForUser");

            migrationBuilder.DropTable(
                name: "NoReadMessages");

            migrationBuilder.DropTable(
                name: "SharedContact");

            migrationBuilder.DropIndex(
                name: "IX_Chats_UserIdentityId",
                table: "Chats");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastActive",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameCu",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StaffId",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
