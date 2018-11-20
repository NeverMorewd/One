USE [IO6]
GO
/****** Object: StoredProcedure [dbo].[csp_insertOldPassword] Script Date: 19/11/2018 9:09:16 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[csp_insertOldPassword] 
        @UserID      uniqueidentifier,
        @Oldpassword nvarchar(50),
        @SetDateTime datetime,
         @DaySpan int
    AS
    BEGIN
        SET NOCOUNT ON;
        DECLARE @LastSetDate                     datetime
        SELECT @LastSetDate = min(SetDateTime) FROM dbo.OldPassword WHERE UserID = @UserID 
        IF(DATEDIFF(hour,@LastSetDate,getdate()) <= @DateTimeSpan)
            BEGIN
                INSERT INTO [dbo].[OldPassword] ([UserID], [OldPassword],[SetDateTime]) 
                VALUES (@UserID, @Oldpassword,@SetDateTime)
            END
ELSE IF((SELECT count(*) FROM dbo.OldPassword WHERE UserID = @UserID) < 10)
         BEGIN
                INSERT INTO [dbo].[OldPassword] ([UserID], [OldPassword],[SetDateTime]) 
                VALUES (@UserID, @Oldpassword,@SetDateTime)
            END
        ELSE
         BEGIN
             UPDATE [dbo].[OldPassword]
                SET [OldPassword] = @OldPassword, [SetDateTime] = @SetDateTime, 
                WHERE UserID = @UserID AND SetDateTime = (SELECT min(SetDateTime) FROM dbo.OldPassword)
                -- DELETE min(SetDateTime) FROM dbo.OldPassword WHERE UserID = @UserID
                -- INSERT INTO [dbo].[OldPassword] ([UserID], [OldPassword],[SetDateTime]) 
                -- VALUES (@UserID, @Oldpassword,@SetDateTime)
            END 
    END
