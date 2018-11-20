USE [IO6]
GO
/****** Object:  StoredProcedure [dbo].[csp_checkOldPassword]    Script Date: 5/11/2018 10:08:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
ALTER PROCEDURE [dbo].[csp_checkOldPassword]
    @UserID                uniqueidentifier,
    @Oldpassword           nvarchar(50),
    @SetDateTime           DATETIME,
    @DaysSpan               INT,
    @CountOfOldPassword    INT

AS
BEGIN
    DECLARE @LastReusedDateTime DATETIME
    DECLARE @number INT
    SET @number = 0
    select @LastReusedDateTime = max(SetDateTime) from [dbo].[csp_checkOldPassword] 
    WHERE UserID = @UserID  
    AND Oldpassword = @Oldpassword
    IF(@LastReusedDateTime IS NULL)
       RETURN 0
    ELSE
       IF(datediff(day,@LastReusedDateTime,@SetDateTime)<@DaysSpan)
        RETURN 1
       ELSE
        BEGIN              
            SELECT @number = count(*) FROM dbo.csp_checkOldPassword 
            WHERE SetDateTime >= @LastReusedDateTime
            IF(@number > @CountOfOldPassword)
                RETURN 0
            ELSE
                RETURN 1
        END

END  
