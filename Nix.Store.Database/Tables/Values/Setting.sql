CREATE TABLE [dbo].[Setting]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1000, 1), 
    [Key] NVARCHAR(100) NOT NULL, 
    [Type] NVARCHAR(256) NOT NULL DEFAULT 'string', 
	[ItemType] NVARCHAR(256) NULL, 
    [Value] NVARCHAR(MAX) NULL, 
    [Body] XML NULL, 
    [GroupId] INT NOT NULL,     
    [MapId] INT NOT NULL, 
    CONSTRAINT [FK_Setting_ToGroup] FOREIGN KEY ([GroupId]) REFERENCES [dbo].[Group]([Id]), 
    CONSTRAINT [FK_Setting_ToSettingMapping] FOREIGN KEY ([MapId]) REFERENCES [dbo].[SettingMapping]([Id])
)
