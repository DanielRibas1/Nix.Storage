CREATE TABLE [dbo].[Group]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1000, 1), 
    [Key] NVARCHAR(50) NULL, 
    [ApplicationId] INT NOT NULL, 
    CONSTRAINT [FK_Group_ToApplication] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Application]([Id])
)
