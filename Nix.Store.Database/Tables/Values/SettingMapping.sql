CREATE TABLE [dbo].[SettingMapping]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1000, 1), 
    [Key] NVARCHAR(100) NOT NULL, 
    [GroupId] INT NOT NULL, 
    CONSTRAINT [FK_SettingMapping_ToGroup] FOREIGN KEY ([GroupId]) REFERENCES [dbo].[Group]([Id])
)
