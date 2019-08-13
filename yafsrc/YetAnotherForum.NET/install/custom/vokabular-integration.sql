-- Add column for mapping Category (Forum) - BookType (Vokabular)
	ALTER TABLE [dbo].[yaf_Category] ADD ExternalID SMALLINT NULL UNIQUE

-- Add column for mapping Forum (Forum) - Category (Vokabular)
	ALTER TABLE [dbo].[yaf_Forum] ADD ExternalID INT NULL

-- Add column for mapping Forum (Forum) - Project (Vokabular)
	ALTER TABLE [dbo].[yaf_Forum] ADD ExternalProjectID BIGINT NULL

	ALTER TABLE [dbo].[yaf_Forum] ALTER COLUMN [Name] nvarchar(255) NOT NULL

	ALTER TABLE [dbo].[yaf_Forum] ALTER COLUMN [Description] nvarchar(512) NULL
