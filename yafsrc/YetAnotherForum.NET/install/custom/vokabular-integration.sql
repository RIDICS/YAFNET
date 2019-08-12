/*
** Put any custom sql you want run on reinstall in here.
*/
/*COMMIT

ALTER DATABASE CURRENT
SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE

SET XACT_ABORT ON
BEGIN TRAN*/

-- Add column for mapping Category (Forum) - BookType (Vokabular)
	ALTER TABLE [dbo].[yaf_Category] ADD ExternalID SMALLINT NULL UNIQUE

-- Add column for mapping Forum (Forum) - Category (Vokabular)
	ALTER TABLE [dbo].[yaf_Forum] ADD ExternalID INT NULL

-- Add column for mapping Forum (Forum) - Project (Vokabular)
	ALTER TABLE [dbo].[yaf_Forum] ADD ExternalProjectID BIGINT NULL

/*COMMIT*/