USE [IO6]
GO
/****** Object:  StoredProcedure [dbo].[csp_insertOldPassword]    Script Date: 19/11/2018 9:09:16 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[csp_insertOldPassword] 
		@UserID                         uniqueidentifier,
	    @Oldpassword                    nvarchar(50),
	    @SetDateTime                    datetime
	AS
	BEGIN
		SET NOCOUNT ON;

		INSERT INTO [dbo].[OldPassword] ([UserID], [OldPassword],[SetDateTime]) 
		VALUES (@UserID, @Oldpassword,@SetDateTime)
                  
	END