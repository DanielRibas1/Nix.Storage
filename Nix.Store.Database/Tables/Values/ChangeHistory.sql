CREATE TABLE [dbo].[ChangeHistory]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1000, 1), 
    [EntityName] NVARCHAR(100) NOT NULL, 
    [OriginalValue] NVARCHAR(MAX) NULL, 
    [ChangedValue] NVARCHAR(MAX) NULL, 
    [Action] NVARCHAR(10) NOT NULL, 
    [AgentName] NCHAR(50) NOT NULL, 
    [ChangeDate] DATETIME2 NOT NULL
)
