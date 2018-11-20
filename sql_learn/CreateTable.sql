create table dbo.OldPassword
(
    ID int IDENTITY (1,1) PRIMARY KEY,
	UserID uniqueidentifier not null,
	OldPassword  nvarchar(50) not null,
	SetDateTime datetime not null,
)
