-- ------------------------------------------------------------------------------------------
-- Copyright AspDotNetStorefront.com.  All Rights Reserved.
-- http://www.aspdotnetstorefront.com
-- For details on this license please visit our homepage at the URL above.
-- THE ABOVE NOTICE MUST REMAIN INTACT.
-- ------------------------------------------------------------------------------------------
-- ------------------------------------------------------------------------------------------
-- Database Upgrade Script:
-- AspDotNetStorefront Version 10.0.0 to Latest Microsoft SQL Server 2008 or higher
-- ------------------------------------------------------------------------------------------

/*********** ASPDOTNETSTOREFRONT v10.0.0 to Latest *******************/
/*                                                                */
/*                                                                */
/* BACKUP YOUR EXISTING DATABASE BEFORE RUNNING THIS SCRIPT!!     */
/* BACKUP YOUR EXISTING DATABASE BEFORE RUNNING THIS SCRIPT!!     */
/* BACKUP YOUR EXISTING DATABASE BEFORE RUNNING THIS SCRIPT!!     */
/* BACKUP YOUR EXISTING DATABASE BEFORE RUNNING THIS SCRIPT!!     */
/*                                                                */
/*                                                                */
/* ************************************************************** */

IF NOT EXISTS(SELECT Name from AppConfig WHERE Name = 'RelatedProducts.NumberDisplayed')
BEGIN
	INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn]) VALUES (0, N'RelatedProducts.NumberDisplayed', N'The maximum number of related products displayed at the bottom of product pages.', N'4', N'integer', NULL, N'DISPLAY', 0, 0, GETDATE(), GETDATE())
END
GO

--Cleanup
DELETE FROM AppConfig WHERE Name = 'Bongo.Extend.Enabled'
DELETE FROM AppConfig WHERE Name = 'Bongo.Extend.Script'
DELETE FROM AppConfig WHERE Name = 'PayPal.Express.UseIntegratedCheckout'
DELETE FROM AppConfig WHERE Name = 'PayPal.Express.IntegratedCheckout.SandboxURL'
DELETE FROM AppConfig WHERE Name = 'PayPal.Express.IntegratedCheckout.LiveURL'

--Recreate PayPal URL AppConfigs with their new URLs
DELETE FROM AppConfig WHERE Name = 'PayPal.Express.LiveURL'
INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn])
VALUES (0, N'PayPal.Express.LiveURL', N'PayPal Express In-Context Checkout Live Site URL. Do not change this value without consulting PayPal support.', N'https://www.paypal.com/checkoutnow', NULL, NULL, N'GATEWAY', 1, 0, GETDATE(), GETDATE())

DELETE FROM AppConfig WHERE Name = 'PayPal.Express.SandboxURL'
INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn])
VALUES (0, N'PayPal.Express.SandboxURL', N'PayPal Express In-Context Checkout Sandbox Site URL. Do not change this value without consulting PayPal support.', N'https://www.sandbox.paypal.com/checkoutnow', NULL, NULL, N'GATEWAY', 1, 0, GETDATE(), GETDATE())

--Update the PayPal callback url for IPN
UPDATE AppConfig
SET ConfigValue = 'paypalnotifications'
WHERE Name = 'PayPal.NotificationURL'
AND ConfigValue = 'paypalnotification.aspx'

UPDATE Store
SET StagingURI = ''
WHERE StagingURI = 'staging.mystore.com'

UPDATE Store
SET ProductionURI = 'www.samplesitename.com'
WHERE ProductionURI = 'www.mystore.com'
GO
/*********** End 10.0.1 Changes *********************/


/*********** Begin 10.0.2 Changes *********************/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_updCustomer]'))
	DROP PROC [dbo].[aspdnsf_updCustomer]
GO
create proc [dbo].[aspdnsf_updCustomer]
	@CustomerID int,
	@CustomerLevelID int = null,
	@Email nvarchar(100) = null,
	@Password nvarchar(250) = null,
	@SaltKey int = null,
	@Gender nvarchar(1) = null,
	@FirstName nvarchar(100) = null,
	@LastName nvarchar(100) = null,
	@Notes nvarchar(max) = null,
	@SkinID int = null,
	@Phone nvarchar(25) = null,
	@AffiliateID int = null,
	@Referrer nvarchar(max) = null,
	@CouponCode nvarchar(50) = null,
	@OkToEmail tinyint = null,
	@IsAdmin tinyint = null,
	@BillingEqualsShipping tinyint = null,
	@LastIPAddress varchar(40) = null,
	@OrderNotes nvarchar(max) = null,
	@RTShipRequest nvarchar(max) = null,
	@RTShipResponse nvarchar(max) = null,
	@OrderOptions nvarchar(max) = null,
	@LocaleSetting nvarchar(10) = null,
	@MicroPayBalance money = null,
	@RecurringShippingMethodID int = null,
	@RecurringShippingMethod nvarchar(100) = null,
	@BillingAddressID int = null,
	@ShippingAddressID int = null,
	@ExtensionData nvarchar(max) = null,
	@FinalizationData nvarchar(max) = null,
	@Deleted tinyint = null,
	@Over13Checked tinyint = null,
	@CurrencySetting nvarchar(10) = null,
	@VATSetting int = null,
	@VATRegistrationID nvarchar(100) = null,
	@StoreCCInDB tinyint = null,
	@IsRegistered tinyint = null,
	@LockedUntil datetime = null,
	@AdminCanViewCC tinyint = null,
	@BadLogin smallint = 0, --only pass -1 = null, 0 = null, or 1: -1 clears the field = null, 0 does nothing = null, 1 increments the field by one
	@Active tinyint = null,
	@PwdChangeRequired tinyint = null,
	@RegisterDate datetime = null,
	@RequestedPaymentMethod  nvarchar(100) = null,
	@ClearSavedCCNumbers bit = 0,
	@StoreID	int = null
AS
SET NOCOUNT ON

DECLARE @OldPwd nvarchar(100), @OldSaltKey int
SELECT @OldPwd = Password, @OldSaltKey = SaltKey FROM dbo.Customer WHERE CustomerID = @CustomerID

UPDATE dbo.Customer
SET
	CustomerLevelID = COALESCE(@CustomerLevelID, CustomerLevelID),
	RegisterDate = COALESCE(@RegisterDate, RegisterDate),
	Email = COALESCE(@Email, Email),
	Password = COALESCE(@Password, Password),
	SaltKey = COALESCE(@SaltKey, SaltKey),
	Gender = COALESCE(@Gender, Gender),
	FirstName = COALESCE(@FirstName, FirstName),
	LastName = COALESCE(@LastName, LastName),
	Notes = COALESCE(@Notes, Notes),
	SkinID = COALESCE(@SkinID, SkinID),
	Phone = COALESCE(@Phone, Phone),
	AffiliateID = COALESCE(@AffiliateID, AffiliateID),
	Referrer = COALESCE(@Referrer, Referrer),
	CouponCode = COALESCE(@CouponCode, CouponCode),
	OkToEmail = COALESCE(@OkToEmail, OkToEmail),
	IsAdmin = COALESCE(@IsAdmin, IsAdmin),
	BillingEqualsShipping = COALESCE(@BillingEqualsShipping, BillingEqualsShipping),
	LastIPAddress = COALESCE(@LastIPAddress, LastIPAddress),
	OrderNotes = COALESCE(@OrderNotes, OrderNotes),
	RTShipRequest = COALESCE(@RTShipRequest, RTShipRequest),
	RTShipResponse = COALESCE(@RTShipResponse, RTShipResponse),
	OrderOptions = COALESCE(@OrderOptions, OrderOptions),
	LocaleSetting = COALESCE(@LocaleSetting, LocaleSetting),
	MicroPayBalance = COALESCE(@MicroPayBalance, MicroPayBalance),
	RecurringShippingMethodID = COALESCE(@RecurringShippingMethodID, RecurringShippingMethodID),
	RecurringShippingMethod = COALESCE(@RecurringShippingMethod, RecurringShippingMethod),
	BillingAddressID = COALESCE(@BillingAddressID, BillingAddressID),
	ShippingAddressID = COALESCE(@ShippingAddressID, ShippingAddressID),
	ExtensionData = COALESCE(@ExtensionData, ExtensionData),
	FinalizationData = COALESCE(@FinalizationData, FinalizationData),
	Deleted = COALESCE(@Deleted, Deleted),
	Over13Checked = COALESCE(@Over13Checked, Over13Checked),
	CurrencySetting = COALESCE(@CurrencySetting, CurrencySetting),
	VATSetting = COALESCE(@VATSetting, VATSetting),
	VATRegistrationID = COALESCE(@VATRegistrationID, VATRegistrationID),
	StoreCCInDB = COALESCE(@StoreCCInDB, StoreCCInDB),
	IsRegistered = COALESCE(@IsRegistered, IsRegistered),
	LockedUntil = COALESCE(@LockedUntil, LockedUntil),
	AdminCanViewCC = COALESCE(@AdminCanViewCC, AdminCanViewCC),
	PwdChanged = CASE
		WHEN @OldPwd <> @Password AND @Password IS NOT NULL THEN getdate()
		ELSE PwdChanged END,
	BadLoginCount = CASE @BadLogin
		WHEN -1 THEN 0
		ELSE BadLoginCount + isnull(@BadLogin, 0) END,
	LastBadLogin = CASE @BadLogin
		WHEN -1 THEN NULL
		WHEN 1 THEN getdate()
		ELSE LastBadLogin END,
	Active = COALESCE(@Active, Active),
	PwdChangeRequired = COALESCE(@PwdChangeRequired, PwdChangeRequired),
	RequestedPaymentMethod = COALESCE(@RequestedPaymentMethod, RequestedPaymentMethod),
	StoreID = COALESCE(@StoreID, StoreID)
WHERE
	CustomerID = @CustomerID
	-- Only update the row if any fields will actually change
	AND (
		@CustomerLevelID is not null and (CustomerLevelID is null or CustomerLevelID != @CustomerLevelID)
		OR @RegisterDate is not null and (RegisterDate is null or RegisterDate != @RegisterDate)
		OR @Email is not null and (Email is null or Email != @Email)
		OR @Password is not null and ([Password] is null or [Password] != @Password)
		OR @SaltKey is not null and (SaltKey is null or SaltKey != @SaltKey)
		OR @Gender is not null and (Gender is null or Gender != @Gender)
		OR @FirstName is not null and (FirstName is null or FirstName != @FirstName)
		OR @LastName is not null and (LastName is null or LastName != @LastName)
		OR @Notes is not null and (Notes is null or Notes != @Notes)
		OR @SkinID is not null and (SkinID is null or SkinID != @SkinID)
		OR @Phone is not null and (Phone is null or Phone != @Phone)
		OR @AffiliateID is not null and (AffiliateID is null or AffiliateID != @AffiliateID)
		OR @Referrer is not null and (Referrer is null or Referrer != @Referrer)
		OR @CouponCode is not null and (CouponCode is null or CouponCode != @CouponCode)
		OR @OkToEmail is not null and (OkToEmail is null or OkToEmail != @OkToEmail)
		OR @IsAdmin is not null and (IsAdmin is null or IsAdmin != @IsAdmin)
		OR @BillingEqualsShipping is not null and (BillingEqualsShipping is null or BillingEqualsShipping != @BillingEqualsShipping)
		OR @LastIPAddress is not null and (LastIPAddress is null or LastIPAddress != @LastIPAddress)
		OR @OrderNotes is not null and (OrderNotes is null or OrderNotes != @OrderNotes)
		OR @RTShipRequest is not null and (RTShipRequest is null or RTShipRequest != @RTShipRequest)
		OR @RTShipResponse is not null and (RTShipResponse is null or RTShipResponse != @RTShipResponse)
		OR @OrderOptions is not null and (OrderOptions is null or OrderOptions != @OrderOptions)
		OR @LocaleSetting is not null and (LocaleSetting is null or LocaleSetting != @LocaleSetting)
		OR @MicroPayBalance is not null and (MicroPayBalance is null or MicroPayBalance != @MicroPayBalance)
		OR @RecurringShippingMethodID is not null and (RecurringShippingMethodID is null or RecurringShippingMethodID != @RecurringShippingMethodID)
		OR @RecurringShippingMethod is not null and (RecurringShippingMethod is null or RecurringShippingMethod != @RecurringShippingMethod)
		OR @BillingAddressID is not null and (BillingAddressID is null or BillingAddressID != @BillingAddressID)
		OR @ShippingAddressID is not null and (ShippingAddressID is null or ShippingAddressID != @ShippingAddressID)
		OR @ExtensionData is not null and (ExtensionData is null or ExtensionData != @ExtensionData)
		OR @FinalizationData is not null and (FinalizationData is null or FinalizationData != @FinalizationData)
		OR @Deleted is not null and (Deleted is null or Deleted != @Deleted)
		OR @Over13Checked is not null and (Over13Checked is null or Over13Checked != @Over13Checked)
		OR @CurrencySetting is not null and (CurrencySetting is null or CurrencySetting != @CurrencySetting)
		OR @VATSetting is not null and (VATSetting is null or VATSetting != @VATSetting)
		OR @VATRegistrationID is not null and (VATRegistrationID is null or VATRegistrationID != @VATRegistrationID)
		OR @StoreCCInDB is not null and (StoreCCInDB is null or StoreCCInDB != @StoreCCInDB)
		OR @IsRegistered is not null and (IsRegistered is null or IsRegistered != @IsRegistered)
		OR @LockedUntil is not null and (LockedUntil is null or LockedUntil != @LockedUntil)
		OR @AdminCanViewCC is not null and (AdminCanViewCC is null or AdminCanViewCC != @AdminCanViewCC)
		OR @Password is not null and (@Password != @OldPwd)
		OR @BadLogin != 0
		OR @PwdChangeRequired is not null and (PwdChangeRequired is null or PwdChangeRequired != @PwdChangeRequired)
		OR @RequestedPaymentMethod is not null and (RequestedPaymentMethod is null or RequestedPaymentMethod != @RequestedPaymentMethod)
		OR @StoreID is not null and (StoreID is null or StoreID != @StoreID)
	)

IF @OldPwd <> @Password and @OldSaltKey <> 0
	INSERT dbo.PasswordLog (CustomerID, OldPwd, SaltKey, ChangeDt)
	VALUES (@CustomerID, @OldPwd, @OldSaltKey, getdate())

IF NULLIF(@ClearSavedCCNumbers, 0) = 1
BEGIN
	UPDATE Address
	SET	CardNumber = NULL,
		CardExpirationMonth = NULL,
		CardExpirationYear = NULL,
		CardStartDate = NULL,
		CardIssueNumber = NULL
	WHERE CustomerID = @CustomerID

	UPDATE Orders
	SET	CardNumber = NULL,
		CardExpirationMonth = NULL,
		CardExpirationYear = NULL,
		CardStartDate = NULL,
		CardIssueNumber = NULL
	WHERE CustomerID = @CustomerID
END
GO
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_updCustomerByEmail]'))
	DROP PROC [dbo].[aspdnsf_updCustomerByEmail]
GO
create proc [dbo].[aspdnsf_updCustomerByEmail]
	@Email nvarchar(100),
	@CustomerLevelID int = null,
	@Password nvarchar(250) = null,
	@SaltKey int = null,
	@Gender nvarchar(1) = null,
	@FirstName nvarchar(100) = null,
	@LastName nvarchar(100) = null,
	@Notes nvarchar(max) = null,
	@SkinID int = null,
	@Phone nvarchar(25) = null,
	@AffiliateID int = null,
	@Referrer nvarchar(max) = null,
	@CouponCode nvarchar(50) = null,
	@OkToEmail tinyint = null,
	@IsAdmin tinyint = null,
	@BillingEqualsShipping tinyint = null,
	@LastIPAddress varchar(40) = null,
	@OrderNotes nvarchar(max) = null,
	@RTShipRequest nvarchar(max) = null,
	@RTShipResponse nvarchar(max) = null,
	@OrderOptions nvarchar(max) = null,
	@LocaleSetting nvarchar(10) = null,
	@MicroPayBalance money = null,
	@RecurringShippingMethodID int = null,
	@RecurringShippingMethod nvarchar(100) = null,
	@BillingAddressID int = null,
	@ShippingAddressID int = null,
	@ExtensionData nvarchar(max) = null,
	@FinalizationData nvarchar(max) = null,
	@Deleted tinyint = null,
	@Over13Checked tinyint = null,
	@CurrencySetting nvarchar(10) = null,
	@VATSetting int = null,
	@VATRegistrationID nvarchar(100) = null,
	@StoreCCInDB tinyint = null,
	@IsRegistered tinyint = null,
	@LockedUntil datetime = null,
	@AdminCanViewCC tinyint = null,
	@BadLogin smallint = 0 , --only pass -1 = null, 0 = null, or 1: -1 clears the field = null, 0 does nothing = null, 1 increments the field by one
	@Active tinyint = null,
	@PwdChangeRequired tinyint = null,
	@RequestedPaymentMethod  nvarchar(100) = null

AS
SET NOCOUNT ON

DECLARE @CustomerID int, @OldPwd nvarchar(100), @IsAdminCust tinyint, @OldSaltKey int

SELECT @CustomerID = CustomerID , @OldPwd = Password, @IsAdminCust = IsAdmin, @OldSaltKey = Saltkey FROM dbo.Customer WHERE Email = @Email


UPDATE dbo.Customer
SET
	CustomerLevelID = COALESCE(@CustomerLevelID, CustomerLevelID),
	Email = COALESCE(@Email, Email),
	Password = COALESCE(@Password, Password),
	SaltKey = COALESCE(@SaltKey, SaltKey),
	Gender = COALESCE(@Gender, Gender),
	FirstName = COALESCE(@FirstName, FirstName),
	LastName = COALESCE(@LastName, LastName),
	Notes = COALESCE(@Notes, Notes),
	SkinID = COALESCE(@SkinID, SkinID),
	Phone = COALESCE(@Phone, Phone),
	AffiliateID = COALESCE(@AffiliateID, AffiliateID),
	Referrer = COALESCE(@Referrer, Referrer),
	CouponCode = COALESCE(@CouponCode, CouponCode),
	OkToEmail = COALESCE(@OkToEmail, OkToEmail),
	IsAdmin = COALESCE(@IsAdmin, IsAdmin),
	BillingEqualsShipping = COALESCE(@BillingEqualsShipping, BillingEqualsShipping),
	LastIPAddress = COALESCE(@LastIPAddress, LastIPAddress),
	OrderNotes = COALESCE(@OrderNotes, OrderNotes),
	RTShipRequest = COALESCE(@RTShipRequest, RTShipRequest),
	RTShipResponse = COALESCE(@RTShipResponse, RTShipResponse),
	OrderOptions = COALESCE(@OrderOptions, OrderOptions),
	LocaleSetting = COALESCE(@LocaleSetting, LocaleSetting),
	MicroPayBalance = COALESCE(@MicroPayBalance, MicroPayBalance),
	RecurringShippingMethodID = COALESCE(@RecurringShippingMethodID, RecurringShippingMethodID),
	RecurringShippingMethod = COALESCE(@RecurringShippingMethod, RecurringShippingMethod),
	BillingAddressID = COALESCE(@BillingAddressID, BillingAddressID),
	ShippingAddressID = COALESCE(@ShippingAddressID, ShippingAddressID),
	ExtensionData = COALESCE(@ExtensionData, ExtensionData),
	FinalizationData = COALESCE(@FinalizationData, FinalizationData),
	Deleted = COALESCE(@Deleted, Deleted),
	Over13Checked = COALESCE(@Over13Checked, Over13Checked),
	CurrencySetting = COALESCE(@CurrencySetting, CurrencySetting),
	VATSetting = COALESCE(@VATSetting, VATSetting),
	VATRegistrationID = COALESCE(@VATRegistrationID, VATRegistrationID),
	StoreCCInDB = COALESCE(@StoreCCInDB, StoreCCInDB),
	IsRegistered = COALESCE(@IsRegistered, IsRegistered),
	LockedUntil = COALESCE(@LockedUntil, LockedUntil),
	AdminCanViewCC = COALESCE(@AdminCanViewCC, AdminCanViewCC),
	PwdChanged = case when @OldPwd <> @Password and @Password is not null then getdate() else PwdChanged end,
	BadLoginCount = case @BadLogin when -1 then 0 else BadLoginCount + @BadLogin end,
	LastBadLogin = case @BadLogin when -1 then null when 1 then getdate() else LastBadLogin end,
	Active = COALESCE(@Active, Active),
	PwdChangeRequired = COALESCE(@PwdChangeRequired, PwdChangeRequired),
	RequestedPaymentMethod = COALESCE(@RequestedPaymentMethod, RequestedPaymentMethod)
WHERE Email = @Email

IF @OldPwd <> @Password and @OldSaltKey <> 0
	INSERT dbo.PasswordLog (CustomerID, OldPwd, SaltKey, ChangeDt)
	VALUES (@CustomerID, @OldPwd, @OldSaltKey, getdate())
GO

-- Update state from ID list to abbreviation list in AppConfig setting 'RTShipping.LocalpickupRestrictionStates'
declare @IDs nvarchar(max)
declare @storeID int
declare @states nvarchar(max)

IF EXISTS (SELECT * FROM AppConfig WHERE Name = 'RTShipping.LocalpickupRestrictionStates')
BEGIN
	DECLARE stateid_cursor CURSOR FOR SELECT ConfigValue, StoreID FROM AppConfig WHERE Name = 'RTShipping.LocalpickupRestrictionStates'
	OPEN stateid_cursor
	FETCH NEXT FROM stateid_cursor INTO @IDs, @storeID

	WHILE @@FETCH_STATUS = 0
	BEGIN
		IF ISNUMERIC((SELECT TOP 1 Items from dbo.Split(@IDs, ','))) > 0 -- check if first element is numeric; if it succeeds, conversion needed, if not, state abbreviation is already used
		BEGIN
			SELECT @states = COALESCE(@states + ',', '') + Abbreviation from State join dbo.Split(@IDs, ',') IDs on IDs.Items = State.StateID
			UPDATE AppConfig SET ConfigValue = @states where Name = 'RTShipping.LocalpickupRestrictionStates' and StoreID = @storeID
			set @states = null
		END
		FETCH NEXT FROM stateid_cursor INTO @IDs, @storeID
	END
END
CLOSE stateid_cursor
DEALLOCATE stateid_cursor

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_Get404Suggestions]'))
	DROP PROC [dbo].[aspdnsf_Get404Suggestions]
GO
CREATE PROC [dbo].[aspdnsf_Get404Suggestions]
	@storeId INT = 0
AS
BEGIN
	SET NOCOUNT ON

	DECLARE @suggestionTypes VARCHAR(100) = (SELECT TOP 1 CASE ConfigValue WHEN '' THEN 'product, category, manufacturer, section, topic' ELSE ConfigValue END FROM [dbo].[AppConfig] WITH(NOLOCK) WHERE [Name] = '404.VisibleSuggestions' AND (StoreID=@storeId OR StoreID=0) ORDER BY StoreID desc)
	DECLARE @hideInventoryLevel INT = (SELECT TOP 1 ConfigValue FROM [dbo].[AppConfig] WITH(NOLOCK) WHERE [Name] = 'HideProductsWithLessThanThisInventoryLevel' AND (StoreID=@storeId OR StoreID=0) ORDER BY StoreID desc)
	
	DECLARE @filterProducts BIT = (SELECT ConfigValue FROM [dbo].[GlobalConfig] WITH(NOLOCK) WHERE [Name] = 'AllowProductFiltering')
	DECLARE @filterTopics BIT = (SELECT ConfigValue FROM [dbo].[GlobalConfig] WITH(NOLOCK) WHERE [Name] = 'AllowTopicFiltering')
	DECLARE @filterEntities BIT = (SELECT ConfigValue FROM [dbo].[GlobalConfig] WITH(NOLOCK) WHERE [Name] = 'AllowEntityFiltering')

	CREATE TABLE #UnfilteredEntities 
	(
		Id INT NOT NULL,
		ObjectType VARCHAR(100) NOT NULL,
		Name NVARCHAR(400) NOT NULL,
		[Description] NVARCHAR(MAX)
	)

	--Products
	IF @suggestionTypes LIKE '%product%'
	BEGIN
		INSERT INTO #UnfilteredEntities(Id, ObjectType, Name, [Description])
		SELECT p.ProductID as id,
			'product', 
			p.Name, 
			p.[Description]
		FROM Product p WITH (NOLOCK)
			INNER JOIN ProductVariant pv ON p.ProductID = pv.ProductID AND pv.IsDefault = 1
			LEFT JOIN (SELECT VariantID, SUM(Quan) AS Inventory
						FROM Inventory
						GROUP BY VariantID) i on pv.VariantID = i.VariantID
			LEFT JOIN ProductStore ps ON p.ProductID = ps.ProductID AND (@filterProducts = 0 OR ps.StoreID = @storeId)
		WHERE p.Deleted = 0
			AND p.Published = 1
			AND pv.Deleted = 0
			AND pv.Published = 1
			AND (CASE p.TrackInventoryBySizeAndColor 
					WHEN 1 THEN ISNULL(i.Inventory, 0) 
					ELSE pv.Inventory 
				END >= @hideInventoryLevel or @hideInventoryLevel = -1)
	END

	--Topics
	IF @suggestionTypes LIKE '%topic%'
	BEGIN
		INSERT INTO #UnfilteredEntities(Id, ObjectType, Name, [Description])
		SELECT t.TopicID as id,
			'topic', 
			t.Name, 
			t.Title
		FROM Topic t
		WHERE t.Published = 1
			AND t.ShowInSiteMap = 1
			AND (@filterTopics = 0 OR t.StoreID = @storeId)
	END

	--Categories
	IF @suggestionTypes LIKE '%category%'
	BEGIN
		INSERT INTO #UnfilteredEntities(Id, ObjectType, Name, [Description])
			SELECT c.CategoryID,
			'category', 
			c.Name,
			c.[Description]
		FROM Category c
		WHERE c.Published = 1
			AND c.Deleted = 0
			AND (@filterEntities = 0 OR (c.CategoryID IN (SELECT DISTINCT EntityID FROM EntityStore WHERE EntityType = 'category' AND StoreID = @storeId)))
	END
	
	--Manufacturers
	IF @suggestionTypes LIKE '%manufacturer%'
	BEGIN
		INSERT INTO #UnfilteredEntities(Id, ObjectType, Name, [Description])
			SELECT m.ManufacturerID,
			'manufacturer', 
			m.Name,
			m.[Description]
		FROM Manufacturer m
		WHERE m.Published = 1
			AND m.Deleted = 0
			AND (@filterEntities = 0 OR (m.ManufacturerID IN (SELECT DISTINCT EntityID FROM EntityStore WHERE EntityType = 'manufacturer' AND StoreID = @storeId)))
	END
	
	--Manufacturers
	IF @suggestionTypes LIKE '%section%'
	BEGIN
		INSERT INTO #UnfilteredEntities(Id, ObjectType, Name, [Description])
			SELECT s.SectionID,
			'section', 
			s.Name,
			s.[Description]
		FROM Section s
		WHERE s.Published = 1
			AND s.Deleted = 0
			AND (@filterEntities = 0 OR (s.SectionID IN (SELECT DISTINCT EntityID FROM EntityStore WHERE EntityType = 'section' AND StoreID = @storeId)))
	END

	SELECT * FROM #UnfilteredEntities
END
GO

-- Add new client resource management AppConfig
if(not exists (select Name from AppConfig where Name='ClientResources.Script.DeferredRenderingEnabled'))
begin
	insert into AppConfig(Name, ConfigValue, Description, GroupName, ValueType)
	values('ClientResources.Script.DeferredRenderingEnabled', 'false', 'Set to true to defer rendering of opted-in scripts to the end of the HTML body tag' , 'CLIENT RESOURCES', 'boolean')
end

-- Insert ShippingCalculationStore records for each missing Store, and only if there is a single selected record in ShippingCalculation
insert into ShippingCalculationStore(StoreID, ShippingCalculationId)
select StoreID, 
	(select ShippingCalculationID from ShippingCalculation where selected = 1) 
from Store where not(StoreID in (select StoreID from ShippingCalculationStore)) 
	and (select count(*) from ShippingCalculation where Selected = 1) = 1
go

-- Fix upsell products sproc not handling ML-encoded size/color
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_GetUpsellProducts]'))
	DROP PROC [dbo].[aspdnsf_GetUpsellProducts]
GO

CREATE PROCEDURE [dbo].[aspdnsf_GetUpsellProducts]
	@productID			INT,
	@customerlevelID	INT,
	@invFilter			INT,
	@storeID			INT = 1,
	@filterProduct		BIT = 0
AS
BEGIN
	SET NOCOUNT ON;

   DECLARE @UpsellProducts VARCHAR(8000),
		   @UpsellProductDiscPct MONEY

	SELECT @UpsellProducts = REPLACE(CAST(UpsellProducts AS VARCHAR(8000)), ' ', '')
		 , @UpsellProductDiscPct = UpsellProductDiscountPercentage
	  FROM dbo.product WITH (NOLOCK) WHERE productid = @productid

	SELECT 1-(@UpsellProductDiscPct/100) UpsellDiscMultiplier
		 , p.*
		 , pv.VariantID
		 , pv.Price
		 , ISNULL(pv.SalePrice, 0) SalePrice
		 , ISNULL(pv.SkuSuffix, '') AS SkuSuffix
		 , ISNULL(pv.ManufacturerPartNumber, '') AS VariantManufacturerPartNumber
		 , ISNULL(pv.Dimensions, '') AS Dimensions
		 , pv.Weight
		 , ISNULL(pv.GTIN, '') AS GTIN
		 , pv.Condition
		 , ISNULL(pv.Points, 0) Points
		 , sp.Name SalesPromptName
		 , ISNULL(ep.price, 0) ExtendedPrice
		 , ProductManufacturer.ManufacturerID AS ProductManufacturerId
		 , Manufacturer.Name AS ProductManufacturerName
		 , Manufacturer.SEName AS ProductManufacturerSEName
	  FROM dbo.product p WITH (NOLOCK)
	  JOIN dbo.split(@UpsellProducts, ',') up ON p.productid = CAST(up.items AS INT)
 LEFT JOIN dbo.SalesPrompt sp WITH (NOLOCK) ON sp.SalesPromptID = p.SalesPromptID
	  JOIN dbo.productvariant pv WITH (NOLOCK) ON pv.productid = CAST(up.items AS INT) AND pv.isdefault = 1 AND pv.Published = 1 AND pv.Deleted = 0
 LEFT JOIN dbo.ExtendedPrice ep WITH (NOLOCK) ON ep.VariantID = pv.VariantID AND ep.CustomerLevelID = @CustomerLevelID
	  JOIN (SELECT p.ProductID
			  FROM dbo.product p WITH (NOLOCK)
			  JOIN dbo.split(@UpsellProducts, ',') rp ON p.productid = CAST(rp.items AS INT)
			  JOIN (SELECT ProductID, SUM(Inventory) Inventory
					  FROM dbo.productvariant WITH (NOLOCK)
				  GROUP BY ProductID) pv ON p.ProductID = pv.ProductID
		 LEFT JOIN (SELECT ProductID
						 , SUM(quan) inventory
					  FROM dbo.inventory i1 WITH (NOLOCK)
					  JOIN dbo.productvariant pv1 WITH (NOLOCK) ON pv1.variantid = i1.variantid
					  JOIN dbo.split(@UpsellProducts, ',') rp1 ON pv1.productid = CAST(rp1.items AS INT)
				  GROUP BY pv1.productid) i ON i.productid = p.productid
				WHERE CASE p.TrackInventoryBySizeAndColor WHEN 1 THEN ISNULL(i.inventory, 0) ELSE pv.inventory END >= @InvFilter
				   ) tp ON p.productid = tp.productid
		INNER JOIN (SELECT DISTINCT a.ProductID
					  FROM Product a WITH (NOLOCK)
				 LEFT JOIN ProductStore b WITH (NOLOCK) ON a.ProductID = b.ProductID WHERE (@filterProduct = 0 OR StoreID = @storeID)
				   ) ps ON p.ProductID = ps.ProductID
LEFT JOIN dbo.ProductManufacturer WITH (NOLOCK) ON tp.ProductID = ProductManufacturer.ProductID
LEFT JOIN dbo.Manufacturer WITH (NOLOCK) ON ProductManufacturer.ManufacturerID = Manufacturer.ManufacturerID
	WHERE p.Published = 1
	  AND p.Deleted = 0
	  AND p.IsCallToOrder = 0
	  AND pv.CustomerEntersPrice = 0
	  AND p.IsAKit = 0
	  AND ( NULLIF(pv.Sizes, '') IS NULL
			OR NOT EXISTS (
				SELECT *
				FROM dbo.ParseMlLocales(pv.Sizes) Sizes
				INNER JOIN LocaleSetting ON Sizes.Locale = LocaleSetting.Name
				WHERE [Value] IS NOT NULL))
	  AND ( NULLIF(pv.Colors, '') IS NULL
			OR NOT EXISTS (
				SELECT *
				FROM dbo.ParseMlLocales(pv.Colors) Colors
				INNER JOIN LocaleSetting ON Colors.Locale = LocaleSetting.Name
				WHERE [Value] IS NOT NULL))
	  AND p.productid != @productid
END
GO

-- Update aspdnsf_getOrder to remove Notes field collision
IF EXISTS (select * from dbo.sysobjects where id = object_id(N'[dbo].[aspdnsf_getOrder]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
	drop proc [dbo].[aspdnsf_getOrder]
GO

CREATE proc [dbo].[aspdnsf_getOrder]
	@ordernumber int
as
set nocount on
select
	o.OrderNumber,
	o.OrderGUID,
	o.ParentOrderNumber,
	o.StoreVersion,
	o.QuoteCheckout,
	o.IsNew,
	o.ShippedOn,
	o.CustomerID,
	o.CustomerGUID,
	o.Referrer,
	o.SkinID,
	o.LastName,
	o.FirstName,
	o.Email,
	o.Notes,
	o.BillingEqualsShipping,
	o.BillingLastName,
	o.BillingFirstName,
	o.BillingCompany,
	o.BillingAddress1,
	o.BillingAddress2,
	o.BillingSuite,
	o.BillingCity,
	o.BillingState,
	o.BillingZip,
	o.BillingCountry,
	o.BillingPhone,
	o.ShippingLastName,
	o.ShippingFirstName,
	o.ShippingCompany,
	o.ShippingResidenceType,
	o.ShippingAddress1,
	o.ShippingAddress2,
	o.ShippingSuite,
	o.ShippingCity,
	o.ShippingState,
	o.ShippingZip,
	o.ShippingCountry,
	o.ShippingMethodID,
	o.ShippingMethod,
	o.ShippingPhone,
	o.ShippingCalculationID,
	o.Phone,
	o.RegisterDate,
	o.AffiliateID,
	o.CouponCode,
	o.CouponType,
	o.CouponDescription,
	o.CouponDiscountAmount,
	o.CouponDiscountPercent,
	o.CouponIncludesFreeShipping,
	o.OkToEmail,
	o.Deleted,
	o.CardType,
	o.CardName,
	o.CardNumber,
	o.CardExpirationMonth,
	o.CardExpirationYear,
	o.OrderSubtotal,
	o.OrderTax,
	o.OrderShippingCosts,
	o.OrderTotal,
	o.PaymentGateway,
	o.AuthorizationCode,
	o.AuthorizationResult,
	o.AuthorizationPNREF,
	o.TransactionCommand,
	o.OrderDate,
	o.LevelID,
	o.LevelName,
	o.LevelDiscountPercent,
	o.LevelDiscountAmount,
	o.LevelHasFreeShipping,
	o.LevelAllowsQuantityDiscounts,
	o.LevelHasNoTax,
	o.LevelAllowsCoupons,
	o.LevelDiscountsApplyToExtendedPrices,
	o.LastIPAddress,
	o.PaymentMethod,
	o.OrderNotes,
	o.PONumber,
	o.DownloadEmailSentOn,
	o.ReceiptEmailSentOn,
	o.DistributorEmailSentOn,
	o.ShippingTrackingNumber,
	o.ShippedVIA,
	o.CustomerServiceNotes,
	o.RTShipRequest,
	o.RTShipResponse,
	o.TransactionState,
	o.AVSResult,
	o.CaptureTXCommand,
	o.CaptureTXResult,
	o.VoidTXCommand,
	o.VoidTXResult,
	o.RefundTXCommand,
	o.RefundTXResult,
	o.CardinalLookupResult,
	o.CardinalAuthenticateResult,
	o.CardinalGatewayParms,
	o.AffiliateCommissionRecorded,
	o.OrderOptions,
	o.OrderWeight,
	o.CarrierReportedRate,
	o.CarrierReportedWeight,
	o.LocaleSetting,
	o.FinalizationData,
	o.ExtensionData,
	o.AlreadyConfirmed,
	o.CartType,
	o.Last4,
	o.ReadyToShip,
	o.IsPrinted,
	o.AuthorizedOn,
	o.CapturedOn,
	o.RefundedOn,
	o.VoidedOn,
	o.EditedOn,
	o.InventoryWasReduced,
	o.MaxMindFraudScore,
	o.MaxMindDetails,
	o.CardStartDate,
	o.CardIssueNumber,
	o.TransactionType,
	o.Crypt,
	o.VATRegistrationID,
	o.FraudedOn,
	o.RefundReason,
	o.AuthorizationPNREF as TransactionID,
	o.RecurringSubscriptionID,
	o.RelatedOrderNumber,
	o.ReceiptHtml,
	os.ShoppingCartRecID,
	os.IsTaxable,
	os.IsShipSeparately,
	os.IsDownload,
	os.DownloadLocation,
	os.FreeShipping,
	os.DistributorID,
	os.ShippingDetail,
	os.TaxClassID,
	os.TaxRate,
	os.Notes as CartNotes,
	os.CustomerEntersPrice,
	os.ProductID,
	os.VariantID,
	os.Quantity,
	os.ChosenColor,
	os.ChosenColorSKUModifier,
	os.ChosenSize,
	os.ChosenSizeSKUModifier,
	os.TextOption,
	os.SizeOptionPrompt,
	os.ColorOptionPrompt,
	os.TextOptionPrompt,
	os.CustomerEntersPricePrompt,
	os.OrderedProductQuantityDiscountID,
	os.OrderedProductQuantityDiscountName,
	os.OrderedProductQuantityDiscountPercent,
	os.OrderedProductName,
	os.OrderedProductVariantName,
	os.OrderedProductSKU,
	os.OrderedProductManufacturerPartNumber,
	os.OrderedProductPrice,
	os.OrderedProductWeight,
	os.OrderedProductPrice,
	os.ShippingMethodID,
	os.ShippingMethodID CartItemShippingMethodID,
	os.ShippingMethod CartItemShippingMethod,
	os.ShippingAddressID,
	os.IsAKit
from Orders o with (nolock)
	left join orders_ShoppingCart os with (nolock) ON os.OrderNumber = o.OrderNumber
where o.OrderNumber = @ordernumber
order by os.ShippingAddressID
GO

-- Moves Profile cleanup to monthly maintenance sproc
IF EXISTS (select * from dbo.sysobjects where id = object_id(N'[dbo].[aspdnsf_MonthlyMaintenance]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
	drop proc [dbo].[aspdnsf_MonthlyMaintenance]
GO

create proc [dbo].[aspdnsf_MonthlyMaintenance]
	@purgeAnonCustomers tinyint = 1,
	@cleanShoppingCartsOlderThan smallint = 30, -- set to 0 to disable erasing
	@cleanWishListsOlderThan smallint = 30, -- set to 0 to disable erasing
	@eraseCCFromAddresses tinyint = 1, -- except those used for recurring billing items!
	@clearProductViewsOrderThan smallint = 180,
	@eraseCCFromOrdersOlderThan smallint = 30, -- set to 0 to disable erasing
	@defragIndexes tinyint = 0,
	@updateStats tinyint = 0,
	@purgeDeletedRecords tinyint = 0,-- Purges records in all tables with a deleted flag set to 1
	@removeRTShippingDataOlderThan smallint = 30, -- set to 0 to disable erasing
	@clearSearchLogOlderThan smallint = 30,-- set to 0 to disable erasing
	@cleanOrphanedLocalizedNames tinyint = 0,
	@cleanupSecurityLog tinyint = 0,
	@clearProfilesOlderThan smallint = 0
	as
begin
	set nocount on

	-- Clear out failed transactions older than 2 months:
	delete from FailedTransaction where OrderDate < dateadd(mm,-2,getdate());

	-- Clear out old tx info, not longer needed:
	update orders set TransactionCommand=NULL, AuthorizationResult=NULL, VoidTXCommand=NULL, VoidTXResult=NULL, CaptureTXCommand=NULL, CaptureTXResult=NULL, RefundTXCommand=NULL, RefundTXResult=NULL where orderdate < dateadd(mm,-2,getdate());

	-- Clean up Security Log entries that are more than 1 year old
	if @cleanupSecurityLog = 1
	begin
		delete from SecurityLog where ActionDate < dateadd(year,-1,getdate());
	end

	-- Clean up data in the LocalizedObjectName table for locales that no longer exist
	if @cleanOrphanedLocalizedNames = 1
	begin
		delete from LocalizedObjectName where LocaleId not in (select LocaleSettingID from LocaleSetting);
	end

	-- clean up all carts (don't touch recurring items or wishlist items however):
	if @cleanShoppingCartsOlderThan <> 0
	begin
		delete dbo.kitcart where (CartType=0 or CartType=101) and CreatedOn < dateadd(d,-@cleanShoppingCartsOlderThan,getdate());
		delete dbo.ShoppingCart where (CartType=0 or CartType=101) and CreatedOn < dateadd(d,-@cleanShoppingCartsOlderThan,getdate());
	end

	if @cleanWishListsOlderThan <> 0
	begin
		delete dbo.kitcart where CartType=1 and CreatedOn < dateadd(d,-@cleanWishListsOlderThan,getdate());
		delete dbo.ShoppingCart where CartType=1 and CreatedOn < dateadd(d,-@cleanWishListsOlderThan,getdate());
	end

	-- purge anon customers:
	if @purgeAnonCustomers = 1
	begin
		-- clean out CIM profiles for orders that were not completed
		delete dbo.CIM_AddressPaymentProfileMap where customerid not in (select customerid from dbo.customer with (NOLOCK) where IsRegistered=1)

		delete dbo.customer where 
			IsRegistered=0 and IsAdmin = 0
			and customerid not in (select customerid from dbo.ShoppingCart with (NOLOCK))
			and customerid not in (select customerid from dbo.kitcart with (NOLOCK))
			and customerid not in (select customerid from dbo.orders with (NOLOCK))
			and customerid not in (select customerid from dbo.rating with (NOLOCK))
			and customerid not in (select ratingcustomerid from dbo.ratingcommenthelpfulness with (NOLOCK))
			and customerid not in (select votingcustomerid from dbo.ratingcommenthelpfulness with (NOLOCK))
	end

	-- clean addresses, except for those that have recurring orders
	if @eraseCCFromAddresses = 1
		update [dbo].address set 
			CardNumber=NULL,
			CardStartDate=NULL,
			CardIssueNumber=NULL,
			CardExpirationMonth=NULL,
			CardExpirationYear=NULL
		where CustomerID not in (select CustomerID from ShoppingCart where CartType=2)

	-- erase credit cards from all orders older than N days:
	if @eraseCCFromOrdersOlderThan <> 0
		update [dbo].orders set CardNumber=NULL
		where
			OrderDate < dateadd(d,-@eraseCCFromOrdersOlderThan,getdate())

	-- erase product views both for dynamic
	if @clearProductViewsOrderThan <> 0
	begin
		delete dbo.ProductView where ViewDate < dateadd(d,-@clearProductViewsOrderThan,getdate())
	end

	-- Nuke deleted stores
	declare @storeId int
	select top 1 @storeId = StoreID from Store where Deleted = 1
	while @@rowcount > 0 begin
		exec aspdnsf_NukeStore @storeId, 0
		select top 1 @storeId = StoreID from Store where Deleted = 1
	end

	if @purgeDeletedRecords = 1 begin
		delete dbo.Address where deleted = 1
		delete dbo.Coupon where deleted = 1
		delete dbo.Customer where deleted = 1
		delete dbo.Document where deleted = 1
		delete dbo.News where deleted = 1
		delete dbo.Product where deleted = 1
		delete dbo.ProductVariant where deleted = 1 or not exists (select * from dbo.Product where productid = ProductVariant.productid)
		delete dbo.SalesPrompt where deleted = 1
		delete dbo.ShippingZone where deleted = 1
		delete dbo.Topic where deleted = 1
		delete dbo.Affiliate where deleted = 1
		delete dbo.Category where deleted = 1
		delete dbo.CustomerLevel where deleted = 1
		delete dbo.Distributor where deleted = 1
		delete dbo.Genre where deleted = 1
		delete dbo.Library where deleted = 1
		delete dbo.Manufacturer where deleted = 1
		delete dbo.Section where deleted = 1
		delete dbo.Vector where deleted = 1
		delete dbo.ProductVector where not exists (select * from dbo.product where productid = ProductVector.productid) or not exists (select * from dbo.vector where vectorid = ProductVector.vectorid)
		delete dbo.ProductAffiliate where not exists (select * from dbo.product where productid = ProductAffiliate.productid) or not exists (select * from dbo.Affiliate where Affiliateid = ProductAffiliate.Affiliateid)
		delete dbo.ProductCategory where not exists (select * from dbo.product where productid = ProductCategory.productid) or not exists (select * from dbo.Category where Categoryid = ProductCategory.Categoryid)
		delete dbo.ProductCustomerLevel where not exists (select * from dbo.product where productid = ProductCustomerLevel.productid) or not exists (select * from dbo.CustomerLevel where CustomerLevelid = ProductCustomerLevel.CustomerLevelid)
		delete dbo.ProductDistributor where not exists (select * from dbo.product where productid = ProductDistributor.productid) or not exists (select * from dbo.Distributor where Distributorid = ProductDistributor.Distributorid)
		delete dbo.ProductGenre where not exists (select * from dbo.product where productid = ProductGenre.productid) or not exists (select * from dbo.Genre where Genreid = ProductGenre.Genreid)
		delete dbo.ProductLocaleSetting where not exists (select * from dbo.product where productid = ProductLocaleSetting.productid) or not exists (select * from dbo.LocaleSetting where LocaleSettingid = ProductLocaleSetting.LocaleSettingid)
		delete dbo.ProductManufacturer where not exists (select * from dbo.product where productid = ProductManufacturer.productid) or not exists (select * from dbo.Manufacturer where Manufacturerid = ProductManufacturer.Manufacturerid)
		delete dbo.ProductSection where not exists (select * from dbo.product where productid = ProductSection.productid) or not exists (select * from dbo.Section where Sectionid = ProductSection.Sectionid)
	end

	-- Clear out all customer sessions
	truncate table CustomerSession

	-- Clean up abandon records tied to customers that no longer exist
	delete from dbo.ShoppingCart where CustomerID not in (select distinct CustomerID from Customer);
	delete from dbo.KitCart where ShoppingCartRecID not in (select distinct ShoppingCartRecID from ShoppingCart);
	delete from dbo.CustomerSession where CustomerID not in (select distinct CustomerID from Customer);
	delete from dbo.RatingCommentHelpfulness where RatingCustomerID not in (select distinct CustomerID from Customer);
	delete from dbo.RatingCommentHelpfulness where VotingCustomerID not in (select distinct CustomerID from Customer);
	delete from dbo.PromotionUsage where CustomerID not in (select distinct CustomerID from Customer);
	delete from dbo.Address where CustomerID not in (select distinct CustomerID from Customer);
	delete from dbo.Rating where CustomerID not in (select distinct CustomerID from Customer);

	-- Remove old rtshipping requests and responses
	if @removeRTShippingDataOlderThan <> 0
	begin
		update dbo.Customer set RTShipRequest = '', RTShipResponse = ''
		where CreatedOn < dateadd(d,-@removeRTShippingDataOlderThan,getdate())

		update dbo.Orders set RTShipRequest = '', RTShipResponse = ''
		where OrderDate < dateadd(d,-@removeRTShippingDataOlderThan,getdate())
	end

	-- Search log
	if @clearSearchLogOlderThan <> 0
	begin
		delete from dbo.SearchLog where CreatedOn < dateadd(d,-@clearSearchLogOlderThan,getdate())
	end

	-- Profile table
	if @clearProfilesOlderThan != -1
	begin
		if @clearProfilesOlderThan = 0
		begin
			truncate table [dbo].[profile]
		end
		else
		begin
			delete from [dbo].[Profile] where UpdatedOn < dateadd(d,-@clearProfilesOlderThan,getdate());
		end
	end

	-- Defrag indexes
	DECLARE @indexesUpdated BIT = 0
	IF @defragIndexes = 1
	BEGIN
		DECLARE @cmd NVARCHAR(MAX), @tableName VARCHAR(128), @indexName VARCHAR(128)
		CREATE TABLE #INDEXESTOUPDATE(
			TableName VARCHAR(128),
			IndexName VARCHAR(128))

		INSERT INTO #IndexesToUpdate
		SELECT  o.name AS TableName, i.name AS IndexName
		FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, NULL) s
			JOIN sys.sysobjects o ON s.object_id = o.id
			LEFT JOIN sys.indexes i ON s.index_id = i.index_id AND o.id = i.object_id
		WHERE s.index_id != 0
			AND s.avg_fragmentation_in_percent > 10
			AND s.page_count > 500
		ORDER BY o.name

		DECLARE indexCursor CURSOR
			FOR SELECT TableName, IndexName FROM #IndexesToUpdate
		OPEN indexCursor
		FETCH NEXT FROM indexCursor
			INTO @tableName, @indexName
		WHILE @@FETCH_STATUS = 0
			BEGIN
				SET @cmd = 'ALTER INDEX [' + @indexName + '] ON [' + @tableName  + '] REBUILD'
				EXEC (@cmd)

			FETCH NEXT FROM indexCursor
				INTO @tableName, @indexName
			END
		CLOSE indexCursor
		DEALLOCATE indexCursor

		-- ALTER INDEX doesn't update statistics, have to trigger it manually
		EXEC [dbo].[aspdnsf_UpdateStatistics]
		SET @indexesUpdated = 1
	END

	--Update statistics, including ones that updating indexes misses
	IF @updateStats = 1 AND @indexesUpdated = 0 --This may have already been done above
	BEGIN
		EXEC [dbo].[aspdnsf_UpdateStatistics]
	END
end
go

-- Clean payment methods of PayPal (Standard) and CheckoutByAmazon, for ConfigValue and AllowableValues
-- Parse out all allowable values for all stores into one homogenzied list
-- Explicitly exclude payment methods deprecated in this version
;with Allowable as (
	select distinct rtrim(ltrim(Items)) [Value]
	from AppConfig
		cross apply dbo.Split(AppConfig.AllowableValues, ',')
	where
		Name = 'PaymentMethods'
		and rtrim(ltrim(Items)) not in ('', 'PayPal', 'CheckoutByAmazon')),


-- Parse out all configured values with their associated store ID
Configured as (
	select StoreID, rtrim(ltrim(Items)) [Value]
	from AppConfig
		cross apply dbo.Split(AppConfig.ConfigValue, ',')
	where
		Name = 'PaymentMethods'),

-- Merge all allowable values for all stores into a single comma-separated list
MergedAllowable as (
	select top 1
		stuff(
			(select ',' + [Value]
			from Allowable
			order by [Value]
			for xml path('')),
			1, 1, '') as [Values]
	from Allowable
	group by [Value]),

-- Merge each store's configured values into a comma-separated list per-store,
-- using the names from the Allowable values and filtering out any configured
-- values that are not in the allowable values
MergedConfigured as (
	select distinct
		StoreID,
		coalesce(stuff(
			(select ',' + Allowable.[Value]
			from Configured
			inner join Allowable on Allowable.Value = Configured.Value
			where Configured.StoreID = StoreWrapper.StoreID
			order by Configured.[Value]
			for xml path('')),
			1, 1, ''), '') as [Values]
	from Configured StoreWrapper
	group by StoreID)

-- Update each store's PaymentMethods AppConfig with the common allowable values and
-- that store's configured values
update AppConfig
set ConfigValue = MergedConfigured.[Values], AllowableValues = MergedAllowable.[Values]
from AppConfig
inner join MergedConfigured on AppConfig.StoreID = MergedConfigured.StoreID
cross join MergedAllowable
where AppConfig.Name = 'PaymentMethods'

/*********** End 10.0.2 Changes *********************/

/*********** Begin 10.0.3 Changes *********************/
-- Add Google Customer Reviews AppConfigs
if not exists(select Name from AppConfig Where Name='GoogleCustomerReviewsMerchantID')
	INSERT INTO AppConfig (AppConfigGUID, StoreID, Name, Description, ConfigValue, ValueType, AllowableValues, GroupName, SuperOnly, Hidden, CreatedOn)
	VALUES(newid(), 0, 'GoogleCustomerReviewsMerchantID', 'This is the ID from your Merchant Center Account. (If you have a Multi-Client Account, also know as an MCA, use the subaccount ID associated with Merchant Center Account that is connected to the domain of your store.)', '', 'string', null, 'GOOGLE CUSTOMER REVIEWS', 0, 0, getdate());

if not exists(select Name from AppConfig Where Name='GoogleCustomerReviewsLanguage')
	INSERT INTO AppConfig (AppConfigGUID, StoreID, Name, Description, ConfigValue, ValueType, AllowableValues, GroupName, SuperOnly, Hidden, CreatedOn)
	VALUES(newid(), 0, 'GoogleCustomerReviewsLanguage', 'This is the language code your badge and opt in survey will use. If the value is left blank, Google will use a language setting based on the user''s browser setting. Visit our manual to get a link to Google''s integration docs where supported language codes for Google Customer Reviews are listed - <a target="blank" style="text-decoration: underline;" href="http://help.aspdotnetstorefront.com/manual/1000/default.aspx#pageid=google_customer_reviews">AspDotNetStorefront Manual</a>.', 'en_US', 'string', null, 'GOOGLE CUSTOMER REVIEWS', 0, 0, getdate());

if not exists(select Name from AppConfig Where Name='GoogleCustomerReviewsEnabled')
	INSERT INTO AppConfig (AppConfigGUID, StoreID, Name, Description, ConfigValue, ValueType, AllowableValues, GroupName, SuperOnly, Hidden, CreatedOn)
	VALUES(newid(), 0, 'GoogleCustomerReviewsEnabled', 'If true, the Google Customer Reviews opt in survey JavaScript will be added to your order confirmation page.', 'false', 'boolean', null, 'GOOGLE CUSTOMER REVIEWS', 0, 0, getdate());
	
if not exists(select Name from AppConfig Where Name='GoogleCustomerReviewsBadgeEnabled')
	INSERT INTO AppConfig (AppConfigGUID, StoreID, Name, Description, ConfigValue, ValueType, AllowableValues, GroupName, SuperOnly, Hidden, CreatedOn)
	VALUES(newid(), 0, 'GoogleCustomerReviewsBadgeEnabled', 'If true, the Google Customer Reviews JavaScript for the badge will be added to every page.', 'true', 'boolean', null, 'GOOGLE CUSTOMER REVIEWS', 0, 0, getdate());

if not exists(select Name from AppConfig Where Name='GoogleCustomerReviewsDeliveryLeadTime')
	INSERT INTO AppConfig (AppConfigGUID, StoreID, Name, Description, ConfigValue, ValueType, AllowableValues, GroupName, SuperOnly, Hidden, CreatedOn)
	VALUES(newid(), 0, 'GoogleCustomerReviewsDeliveryLeadTime', 'The estimated average number of days before an order is delivered (whole numbers only).  Be as accurate as possible without shorting this value as this is used when calculating the estimated delivery date that is passed to Google.', '', 'integer', null, 'GOOGLE CUSTOMER REVIEWS', 0, 0, getdate());

if not exists(select Name from AppConfig Where Name='GoogleCustomerReviewsBadgePosition')
	INSERT INTO AppConfig (AppConfigGUID, StoreID, Name, Description, ConfigValue, ValueType, AllowableValues, GroupName, SuperOnly, Hidden, CreatedOn)
	VALUES(newid(), 0, 'GoogleCustomerReviewsBadgePosition', 'Where to place the Google Customer Reviews badge on your site - bottom right, or bottom left.', 'BOTTOM_RIGHT', 'enum', 'BOTTOM_RIGHT,BOTTOM_LEFT', 'GOOGLE CUSTOMER REVIEWS', 0, 0, getdate());
	
if not exists(select Name from AppConfig Where Name='GoogleCustomerReviewsOptInSurveyPosition')
	INSERT INTO AppConfig (AppConfigGUID, StoreID, Name, Description, ConfigValue, ValueType, AllowableValues, GroupName, SuperOnly, Hidden, CreatedOn)
	VALUES(newid(), 0, 'GoogleCustomerReviewsOptInSurveyPosition', 'Where to place the Google Customer Reviews survey that displays on the order confirmation page - center, bottom right, bottom left, bottom, top right or top left.', 'CENTER_DIALOG', 'enum', 'CENTER_DIALOG,BOTTOM_RIGHT_DIALOG,BOTTOM_LEFT_DIALOG,BOTTOM_TRAY,TOP_RIGHT_DIALOG,TOP_LEFT_DIALOG', 'GOOGLE CUSTOMER REVIEWS', 0, 0, getdate());
	
-- Remove Google Trusted Stores AppConfigs
DELETE FROM AppConfig WHERE Name in(
	'GoogleTrustedStoreID',
	'GoogleTrustedStoreBadgePosition',
	'GoogleTrustedStoreProductSearchID',
	'GoogleTrustedStoreCountry',
	'GoogleTrustedStoreLanguage',
	'GoogleTrustedStoreEnabled',
	'GoogleTrustedStoreProductIdentifierEnabled',
	'GoogleTrustedStoreProductIdentifierFormat',
	'GoogleTrustedStoreShippingLeadTime',
	'GoogleTrustedStoreDeliveryLeadTime')

/*********** End 10.0.3 Changes *********************/

/*********** Begin 10.0.4 Changes *******************/
	
--Upsell products performance improvements
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_GetUpsellProducts]'))
	DROP PROC [dbo].[aspdnsf_GetUpsellProducts]
GO

CREATE PROCEDURE [dbo].[aspdnsf_GetUpsellProducts]
	@productId			INT,
	@customerlevelId	INT,
	@invFilter			INT,
	@storeId			INT = 1,
	@filterProduct		BIT = 0
AS
BEGIN
	SET NOCOUNT ON;
	
	DECLARE @upsellProducts VARCHAR(8000), @UpsellProductDiscountPercentage MONEY

	SELECT @upsellProducts = REPLACE(UpsellProducts, ' ', ''), 
		@upsellProductDiscountPercentage = UpsellProductDiscountPercentage
	FROM dbo.product WITH (NOLOCK) 
	WHERE productid = @productId

	--Bail early if no upsells are set
	IF @upsellProducts IS NULL OR @upsellProducts = ''
	BEGIN
		return
	END

	DECLARE @upsellProductsTable TABLE (ProductId INT NOT NULL)
	INSERT INTO @upsellProductsTable SELECT DISTINCT * FROM dbo.Split(@upsellProducts, ',')

	SELECT 1 - (@upsellProductDiscountPercentage/100) AS UpsellDiscMultiplier,
		p.ProductID,
		p.Name,
		p.Summary,
		p.[Description],
		p.SEAltText,
		p.HidePriceUntilCart,
		p.SEName,
		p.TaxClassID,
		pv.VariantID,
		pv.Price,
		ISNULL(pv.SalePrice, 0) SalePrice,
		ISNULL(pv.SkuSuffix, '') AS SkuSuffix,
		ISNULL(pv.ManufacturerPartNumber, '') AS VariantManufacturerPartNumber,
		ISNULL(pv.Dimensions, '') AS Dimensions,
		pv.Weight,
		ISNULL(pv.GTIN, '') AS GTIN,
		pv.Condition,
		ISNULL(pv.Points, 0) Points,
		sp.Name SalesPromptName,
		ISNULL(ep.price, 0) ExtendedPrice,
		pm.ManufacturerID AS ProductManufacturerId,
		m.Name AS ProductManufacturerName,
		m.SEName AS ProductManufacturerSEName
	FROM dbo.product p WITH (NOLOCK)
		JOIN @upsellProductsTable up ON p.productid = up.ProductId 
			AND p.Published = 1 
			AND p.Deleted = 0 
			AND p.IsCalltoOrder = 0 
			AND p.IsAKit = 0
			AND p.ShowBuyButton = 1
		LEFT JOIN dbo.SalesPrompt sp WITH (NOLOCK) ON sp.SalesPromptID = p.SalesPromptID
		JOIN dbo.ProductVariant pv WITH (NOLOCK) ON pv.ProductID = up.ProductId 
			AND pv.IsDefault = 1 
			AND pv.Published = 1 
			AND pv.Deleted = 0 
			AND pv.CustomerEntersPrice = 0
		LEFT JOIN dbo.ExtendedPrice ep WITH (NOLOCK) ON ep.VariantID = pv.VariantID AND ep.CustomerLevelID = @customerlevelId
		LEFT JOIN (SELECT VariantID, SUM(Quan) Quantity FROM Inventory WITH (NOLOCK) GROUP BY VariantID) i ON pv.VariantID = i.VariantID
		INNER JOIN (
			SELECT DISTINCT a.ProductID
			FROM Product a WITH (NOLOCK)
				LEFT JOIN ProductStore b WITH (NOLOCK) ON a.ProductID = b.ProductID 
			WHERE (@filterProduct = 0 OR StoreID = @storeId)
			) ps ON p.ProductID = ps.ProductID
		LEFT JOIN dbo.ProductManufacturer pm WITH (NOLOCK) ON up.ProductID = pm.ProductID
		LEFT JOIN dbo.Manufacturer m WITH (NOLOCK) ON pm.ManufacturerID = m.ManufacturerID
	WHERE (NULLIF(pv.Sizes, '') IS NULL
			OR NOT EXISTS (
				SELECT *
				FROM dbo.ParseMlLocales(pv.Sizes) Sizes
					INNER JOIN LocaleSetting ON Sizes.Locale = LocaleSetting.Name
				WHERE [Value] IS NOT NULL)
			)
		AND (NULLIF(pv.Colors, '') IS NULL
			OR NOT EXISTS (
				SELECT *
				FROM dbo.ParseMlLocales(pv.Colors) Colors
					INNER JOIN LocaleSetting ON Colors.Locale = LocaleSetting.Name
				WHERE [Value] IS NOT NULL)
			)
		AND p.ProductID != @productId
		AND CASE p.TrackInventoryBySizeAndColor WHEN 1 THEN ISNULL(i.Quantity, 0) ELSE pv.Inventory END >= @invFilter
END
GO

--Upsell products for cart - exclude upsells where Show Buy Button = false
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_GetUpsellProductsForCart]'))
	DROP PROC [dbo].[aspdnsf_GetUpsellProductsForCart]
GO

CREATE PROCEDURE [dbo].[aspdnsf_GetUpsellProductsForCart]
	@customerId			INT,
	@customerlevelId	INT,
	@invFilter			INT,
	@storeId			INT = 1,
	@upsellProductsLimitNumberOnCart			INT = 4,
	@filterProduct		BIT = 0
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @tmp TABLE (ProductId INT, UpsellProductID INT)
	DECLARE @upsellProducts VARCHAR(8000), @pId INT
	SET @upsellProducts = ''

	SELECT TOP 1 @pId = p.ProductID, @upsellProducts = ISNULL(CAST(UpsellProducts AS VARCHAR(8000)), '')
	FROM ShoppingCart sc WITH (NOLOCK) JOIN Product p WITH (NOLOCK) ON sc.ProductID = p.ProductID
	WHERE CustomerID = @customerId
	ORDER BY sc.ProductID

	WHILE @@rowcount > 0 BEGIN
		INSERT @tmp SELECT @pId, CONVERT(INT, s.items) UpsellProductID FROM dbo.split(@UpsellProducts , ',') s LEFT JOIN @tmp t on t.UpsellProductID = CONVERT(INT, s.Items) WHERE t.ProductId IS NULL

		SELECT TOP 1 @pId = p.ProductID, @upsellProducts = ISNULL(CAST(UpsellProducts AS VARCHAR(8000)), '')
		FROM ShoppingCart sc WITH (NOLOCK) JOIN Product p WITH (NOLOCK) ON sc.ProductID = p.ProductID
		WHERE CustomerID = @customerId AND sc.ProductID > @pId
		ORDER BY sc.ProductID
	END

	--For multi store. Delete items not included in a specific store.
	IF (@filterProduct = 1)
	BEGIN
		DELETE @tmp WHERE UpsellProductID NOT IN (SELECT ProductID FROM ProductStore WHERE StoreID = @storeId)
	END

	SELECT TOP (@upsellProductsLimitNumberOnCart) 1-(p2.UpsellProductDiscountPercentage/100) UpsellDiscMultiplier,
			p.ProductID, p.Name, p.SEName, p.Summary, p.Description, RTRIM(pv.Name) VariantName, p.SEAltText, p.ImageFilenameOverride, p.TaxClassID,
			p.SKU, pv.VariantID, p.HidePriceUntilCart, pv.Price, ISNULL(pv.SalePrice, 0) SalePrice, ISNULL(ep.Price, 0) ExtendedPrice, pv.Points,
			sp.Name SalesPromptName
	FROM @tmp t
		JOIN Product p WITH (NOLOCK) ON p.ProductID  = t.UpsellProductID
		JOIN Product p2 WITH (NOLOCK) ON p2.ProductID  = t.ProductId
		JOIN ProductVariant pv WITH (NOLOCK) ON p.ProductID = pv.productid AND pv.IsDefault = 1
		LEFT JOIN SalesPrompt sp  with (NOLOCK) ON p.SalesPromptID = sp.SalesPromptID
		LEFT JOIN ExtendedPrice ep  with (NOLOCK) ON ep.VariantID = pv.VariantID and ep.CustomerLevelID = @customerlevelId
		LEFT JOIN (SELECT VariantID, SUM(quan) quan FROM Inventory WITH (NOLOCK) GROUP BY VariantID) i ON i.VariantID = pv.VariantID
	where (p.RequiresTextOption IS NULL OR p.RequiresTextOption=0)
		AND (ISNULL(CONVERT(NVARCHAR(4000), Sizes), '') = '' OR CONVERT(NVARCHAR(4000), Sizes) NOT LIKE '%>[^<>]%_[^<>]%<%')
		AND (ISNULL(CONVERT(NVARCHAR(4000), Colors), '') = '' OR CONVERT(NVARCHAR(4000), Colors) NOT LIKE '%>[^<>]%_[^<>]%<%')
		AND p.IsAKit=0 AND p.IsCallToOrder = 0 AND pv.CustomerEntersPrice = 0
		AND (pv.Sizes = '' OR pv.Sizes IS NULL) AND (pv.Colors = '' OR pv.Colors IS NULL)
		AND p.ProductID NOT IN (SELECT ProductID FROM ShoppingCart  WITH (NOLOCK) WHERE CartType=0 and CustomerID=@customerId)
		AND CASE p.TrackInventoryBySizeAndColor WHEN 1 THEN ISNULL(i.quan, 0) ELSE pv.inventory END >= @invFilter
		AND p.Published > 0
		AND p.ShowBuyButton = 1
END
GO

--Expand the column so it can handle 50 chars from Product + 50 chars from ProductVariant
ALTER TABLE Orders_ShoppingCart ALTER COLUMN OrderedProductManufacturerPartNumber NVARCHAR(100) NULL
GO

/*********** End 10.0.4 Changes *********************/

/*********** Begin 10.0.5 Changes *******************/
--Extend the Orders.AuthorizationPNREF field to prevent truncate errors
ALTER TABLE Orders ALTER COLUMN AuthorizationPNREF NVARCHAR(MAX)

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_updOrders]'))
	DROP PROC [dbo].[aspdnsf_updOrders]
GO

create proc dbo.aspdnsf_updOrders
    @OrderNumber int,
    @ParentOrderNumber int = null,
    @StoreVersion nvarchar(100) = null,
    @QuoteCheckout tinyint = null,
    @IsNew tinyint = null,
    @ShippedOn datetime = null,
    @CustomerID int = null,
    @CustomerGUID uniqueidentifier = null,
    @Referrer nvarchar(max) = null,
    @SkinID int = null,
    @LastName nvarchar(100) = null,
    @FirstName nvarchar(100) = null,
    @Email nvarchar(100) = null,
    @Notes nvarchar(max) = null,
    @BillingEqualsShipping tinyint = null,
    @BillingLastName nvarchar(100) = null,
    @BillingFirstName nvarchar(100) = null,
    @BillingCompany nvarchar(100) = null,
    @BillingAddress1 nvarchar(100) = null,
    @BillingAddress2 nvarchar(100) = null,
    @BillingSuite nvarchar(50) = null,
    @BillingCity nvarchar(100) = null,
    @BillingState nvarchar(100) = null,
    @BillingZip nvarchar(10) = null,
    @BillingCountry nvarchar(100) = null,
    @BillingPhone nvarchar(25) = null,
    @ShippingLastName nvarchar(100) = null,
    @ShippingFirstName nvarchar(100) = null,
    @ShippingCompany nvarchar(100) = null,
    @ShippingResidenceType int = null,
    @ShippingAddress1 nvarchar(100) = null,
    @ShippingAddress2 nvarchar(100) = null,
    @ShippingSuite nvarchar(50) = null,
    @ShippingCity nvarchar(100) = null,
    @ShippingState nvarchar(100) = null,
    @ShippingZip nvarchar(10) = null,
    @ShippingCountry nvarchar(100) = null,
    @ShippingMethodID int = null,
    @ShippingMethod nvarchar(max) = null,
    @ShippingPhone nvarchar(25) = null,
    @ShippingCalculationID int = null,
    @Phone nvarchar(20) = null,
    @RegisterDate datetime = null,
    @AffiliateID int = null,
    @CouponCode nvarchar(50) = null,
    @CouponType int = null,
    @CouponDescription nvarchar(max) = null,
    @CouponDiscountAmount money = null,
    @CouponDiscountPercent money = null,
    @CouponIncludesFreeShipping tinyint = null,
    @OkToEmail tinyint = null,
    @Deleted tinyint = null,
    @CardType nvarchar(20) = null,
    @CardName nvarchar(100) = null,
    @CardNumber nvarchar(max) = null,
    @CardExpirationMonth nvarchar(10) = null,
    @CardExpirationYear nvarchar(10) = null,
    @OrderSubtotal money = null,
    @OrderTax money = null,
    @OrderShippingCosts money = null,
    @OrderTotal money = null,
    @PaymentGateway nvarchar(50) = null,
    @AuthorizationCode nvarchar(100) = null,
    @AuthorizationResult nvarchar(max) = null,
    @AuthorizationPNREF nvarchar(max) = null,
    @TransactionCommand nvarchar(max) = null,
    @OrderDate datetime = null,
    @LevelID int = null,
    @LevelName nvarchar(100) = null,
    @LevelDiscountPercent money = null,
    @LevelDiscountAmount money = null,
    @LevelHasFreeShipping tinyint = null,
    @LevelAllowsQuantityDiscounts tinyint = null,
    @LevelHasNoTax tinyint = null,
    @LevelAllowsCoupons tinyint = null,
    @LevelDiscountsApplyToExtendedPrices tinyint = null,
    @LastIPAddress varchar(40) = null,
    @PaymentMethod nvarchar(100) = null,
    @OrderNotes nvarchar(max) = null,
    @PONumber nvarchar(50) = null,
    @DownloadEmailSentOn datetime = null,
    @ReceiptEmailSentOn datetime = null,
    @DistributorEmailSentOn datetime = null,
    @ShippingTrackingNumber nvarchar(100) = null,
    @ShippedVIA nvarchar(100) = null,
    @CustomerServiceNotes nvarchar(max) = null,
    @RTShipRequest nvarchar(max) = null,
    @RTShipResponse nvarchar(max) = null,
    @TransactionState nvarchar(20) = null,
    @AVSResult nvarchar(50) = null,
    @CaptureTXCommand nvarchar(max) = null,
    @CaptureTXResult nvarchar(max) = null,
    @VoidTXCommand nvarchar(max) = null,
    @VoidTXResult nvarchar(max) = null,
    @RefundTXCommand nvarchar(max) = null,
    @RefundTXResult nvarchar(max) = null,
    @CardinalLookupResult nvarchar(max) = null,
    @CardinalAuthenticateResult nvarchar(max) = null,
    @CardinalGatewayParms nvarchar(max) = null,
    @AffiliateCommissionRecorded tinyint = null,
    @OrderOptions nvarchar(max) = null,
    @OrderWeight money = null,
    @CarrierReportedRate nvarchar(max) = null,
    @CarrierReportedWeight nvarchar(max) = null,
    @LocaleSetting nvarchar(10) = null,
    @FinalizationData nvarchar(max) = null,
    @ExtensionData nvarchar(max) = null,
    @AlreadyConfirmed tinyint = null,
    @CartType int = null,
    @THUB_POSTED_TO_ACCOUNTING char(1) = null,
    @THUB_POSTED_DATE datetime = null,
    @THUB_ACCOUNTING_REF char(25) = null,
    @Last4 nvarchar(4) = null,
    @ReadyToShip tinyint = null,
    @IsPrinted tinyint = null,
    @AuthorizedOn datetime = null,
    @CapturedOn datetime = null,
    @RefundedOn datetime = null,
    @VoidedOn datetime = null,
    @InventoryWasReduced int = null,
    @MaxMindFraudScore decimal(5, 2) = null,
    @MaxMindDetails nvarchar(max) = null,
    @CardStartDate nvarchar(20) = null,
    @CardIssueNumber nvarchar(25) = null,
    @TransactionType int = null,
    @Crypt int = null,
    @VATRegistrationID nvarchar(max) = null,
    @FraudedOn tinyint = null,
    @RefundReason nvarchar(max) = null
as
set nocount on

update dbo.Orders
set
	ParentOrderNumber = COALESCE(@ParentOrderNumber, ParentOrderNumber),
	StoreVersion = COALESCE(@StoreVersion, StoreVersion),
	QuoteCheckout = COALESCE(@QuoteCheckout, QuoteCheckout),
	IsNew = COALESCE(@IsNew, IsNew),
	ShippedOn = COALESCE(@ShippedOn, ShippedOn),
	CustomerID = COALESCE(@CustomerID, CustomerID),
	CustomerGUID = COALESCE(@CustomerGUID, CustomerGUID),
	Referrer = COALESCE(@Referrer, Referrer),
	SkinID = COALESCE(@SkinID, SkinID),
	LastName = COALESCE(@LastName, LastName),
	FirstName = COALESCE(@FirstName, FirstName),
	Email = COALESCE(@Email, Email),
	Notes = COALESCE(@Notes, Notes),
	BillingEqualsShipping = COALESCE(@BillingEqualsShipping, BillingEqualsShipping),
	BillingLastName = COALESCE(@BillingLastName, BillingLastName),
	BillingFirstName = COALESCE(@BillingFirstName, BillingFirstName),
	BillingCompany = COALESCE(@BillingCompany, BillingCompany),
	BillingAddress1 = COALESCE(@BillingAddress1, BillingAddress1),
	BillingAddress2 = COALESCE(@BillingAddress2, BillingAddress2),
	BillingSuite = COALESCE(@BillingSuite, BillingSuite),
	BillingCity = COALESCE(@BillingCity, BillingCity),
	BillingState = COALESCE(@BillingState, BillingState),
	BillingZip = COALESCE(@BillingZip, BillingZip),
	BillingCountry = COALESCE(@BillingCountry, BillingCountry),
	BillingPhone = COALESCE(@BillingPhone, BillingPhone),
	ShippingLastName = COALESCE(@ShippingLastName, ShippingLastName),
	ShippingFirstName = COALESCE(@ShippingFirstName, ShippingFirstName),
	ShippingCompany = COALESCE(@ShippingCompany, ShippingCompany),
	ShippingResidenceType = COALESCE(@ShippingResidenceType, ShippingResidenceType),
	ShippingAddress1 = COALESCE(@ShippingAddress1, ShippingAddress1),
	ShippingAddress2 = COALESCE(@ShippingAddress2, ShippingAddress2),
	ShippingSuite = COALESCE(@ShippingSuite, ShippingSuite),
	ShippingCity = COALESCE(@ShippingCity, ShippingCity),
	ShippingState = COALESCE(@ShippingState, ShippingState),
	ShippingZip = COALESCE(@ShippingZip, ShippingZip),
	ShippingCountry = COALESCE(@ShippingCountry, ShippingCountry),
	ShippingMethodID = COALESCE(@ShippingMethodID, ShippingMethodID),
	ShippingMethod = COALESCE(@ShippingMethod, ShippingMethod),
	ShippingPhone = COALESCE(@ShippingPhone, ShippingPhone),
	ShippingCalculationID = COALESCE(@ShippingCalculationID, ShippingCalculationID),
	Phone = COALESCE(@Phone, Phone),
	RegisterDate = COALESCE(@RegisterDate, RegisterDate),
	AffiliateID = COALESCE(@AffiliateID, AffiliateID),
	CouponCode = COALESCE(@CouponCode, CouponCode),
	CouponType = COALESCE(@CouponType, CouponType),
	CouponDescription = COALESCE(@CouponDescription, CouponDescription),
	CouponDiscountAmount = COALESCE(@CouponDiscountAmount, CouponDiscountAmount),
	CouponDiscountPercent = COALESCE(@CouponDiscountPercent, CouponDiscountPercent),
	CouponIncludesFreeShipping = COALESCE(@CouponIncludesFreeShipping, CouponIncludesFreeShipping),
	OkToEmail = COALESCE(@OkToEmail, OkToEmail),
	Deleted = COALESCE(@Deleted, Deleted),
	CardType = COALESCE(@CardType, CardType),
	CardName = COALESCE(@CardName, CardName),
	CardNumber = COALESCE(@CardNumber, CardNumber),
	CardExpirationMonth = COALESCE(@CardExpirationMonth, CardExpirationMonth),
	CardExpirationYear = COALESCE(@CardExpirationYear, CardExpirationYear),
	OrderSubtotal = COALESCE(@OrderSubtotal, OrderSubtotal),
	OrderTax = COALESCE(@OrderTax, OrderTax),
	OrderShippingCosts = COALESCE(@OrderShippingCosts, OrderShippingCosts),
	OrderTotal = COALESCE(@OrderTotal, OrderTotal),
	PaymentGateway = COALESCE(@PaymentGateway, PaymentGateway),
	AuthorizationCode = COALESCE(@AuthorizationCode, AuthorizationCode),
	AuthorizationResult = COALESCE(@AuthorizationResult, AuthorizationResult),
	AuthorizationPNREF = COALESCE(@AuthorizationPNREF, AuthorizationPNREF),
	TransactionCommand = COALESCE(@TransactionCommand, TransactionCommand),
	OrderDate = COALESCE(@OrderDate, OrderDate),
	LevelID = COALESCE(@LevelID, LevelID),
	LevelName = COALESCE(@LevelName, LevelName),
	LevelDiscountPercent = COALESCE(@LevelDiscountPercent, LevelDiscountPercent),
	LevelDiscountAmount = COALESCE(@LevelDiscountAmount, LevelDiscountAmount),
	LevelHasFreeShipping = COALESCE(@LevelHasFreeShipping, LevelHasFreeShipping),
	LevelAllowsQuantityDiscounts = COALESCE(@LevelAllowsQuantityDiscounts, LevelAllowsQuantityDiscounts),
	LevelHasNoTax = COALESCE(@LevelHasNoTax, LevelHasNoTax),
	LevelAllowsCoupons = COALESCE(@LevelAllowsCoupons, LevelAllowsCoupons),
	LevelDiscountsApplyToExtendedPrices = COALESCE(@LevelDiscountsApplyToExtendedPrices, LevelDiscountsApplyToExtendedPrices),
	LastIPAddress = COALESCE(@LastIPAddress, LastIPAddress),
	PaymentMethod = COALESCE(@PaymentMethod, PaymentMethod),
	OrderNotes = COALESCE(@OrderNotes, OrderNotes),
	PONumber = COALESCE(@PONumber, PONumber),
	DownloadEmailSentOn = COALESCE(@DownloadEmailSentOn, DownloadEmailSentOn),
	ReceiptEmailSentOn = COALESCE(@ReceiptEmailSentOn, ReceiptEmailSentOn),
	DistributorEmailSentOn = COALESCE(@DistributorEmailSentOn, DistributorEmailSentOn),
	ShippingTrackingNumber = COALESCE(@ShippingTrackingNumber, ShippingTrackingNumber),
	ShippedVIA = COALESCE(@ShippedVIA, ShippedVIA),
	CustomerServiceNotes = COALESCE(@CustomerServiceNotes, CustomerServiceNotes),
	RTShipRequest = COALESCE(@RTShipRequest, RTShipRequest),
	RTShipResponse = COALESCE(@RTShipResponse, RTShipResponse),
	TransactionState = COALESCE(@TransactionState, TransactionState),
	AVSResult = COALESCE(@AVSResult, AVSResult),
	CaptureTXCommand = COALESCE(@CaptureTXCommand, CaptureTXCommand),
	CaptureTXResult = COALESCE(@CaptureTXResult, CaptureTXResult),
	VoidTXCommand = COALESCE(@VoidTXCommand, VoidTXCommand),
	VoidTXResult = COALESCE(@VoidTXResult, VoidTXResult),
	RefundTXCommand = COALESCE(@RefundTXCommand, RefundTXCommand),
	RefundTXResult = COALESCE(@RefundTXResult, RefundTXResult),
	CardinalLookupResult = COALESCE(@CardinalLookupResult, CardinalLookupResult),
	CardinalAuthenticateResult = COALESCE(@CardinalAuthenticateResult, CardinalAuthenticateResult),
	CardinalGatewayParms = COALESCE(@CardinalGatewayParms, CardinalGatewayParms),
	AffiliateCommissionRecorded = COALESCE(@AffiliateCommissionRecorded, AffiliateCommissionRecorded),
	OrderOptions = COALESCE(@OrderOptions, OrderOptions),
	OrderWeight = COALESCE(@OrderWeight, OrderWeight),
	CarrierReportedRate = COALESCE(@CarrierReportedRate, CarrierReportedRate),
	CarrierReportedWeight = COALESCE(@CarrierReportedWeight, CarrierReportedWeight),
	LocaleSetting = COALESCE(@LocaleSetting, LocaleSetting),
	FinalizationData = COALESCE(@FinalizationData, FinalizationData),
	ExtensionData = COALESCE(@ExtensionData, ExtensionData),
	AlreadyConfirmed = COALESCE(@AlreadyConfirmed, AlreadyConfirmed),
	CartType = COALESCE(@CartType, CartType),
	Last4 = COALESCE(@Last4, Last4),
	ReadyToShip = COALESCE(@ReadyToShip, ReadyToShip),
	IsPrinted = COALESCE(@IsPrinted, IsPrinted),
	AuthorizedOn = COALESCE(@AuthorizedOn, AuthorizedOn),
	CapturedOn = COALESCE(@CapturedOn, CapturedOn),
	RefundedOn = COALESCE(@RefundedOn, RefundedOn),
	VoidedOn = COALESCE(@VoidedOn, VoidedOn),
	InventoryWasReduced = COALESCE(@InventoryWasReduced, InventoryWasReduced),
	MaxMindFraudScore = COALESCE(@MaxMindFraudScore, MaxMindFraudScore),
	MaxMindDetails = COALESCE(@MaxMindDetails, MaxMindDetails),
	CardStartDate = COALESCE(@CardStartDate, CardStartDate),
	CardIssueNumber = COALESCE(@CardIssueNumber, CardIssueNumber),
	TransactionType = COALESCE(@TransactionType, TransactionType),
	Crypt = COALESCE(@Crypt, Crypt),
	VATRegistrationID = COALESCE(@VATRegistrationID, VATRegistrationID),
	FraudedOn = COALESCE(@FraudedOn, FraudedOn),
	RefundReason = COALESCE(@RefundReason, RefundReason)
where OrderNumber = @OrderNumber
GO

--Licensing Changes
IF NOT EXISTS(SELECT Name from GlobalConfig WHERE Name = 'LicenseServiceUrl')
BEGIN
	INSERT INTO [dbo].[GlobalConfig] ([Name], [Description], [ConfigValue], [ValueType], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn]) VALUES (N'LicenseServiceUrl', N'Do not change this value.', N'https://licensecheck.api.aspdotnetstorefront.com/licensevalidation/validatelicense', N'string', N'LICENSING', 0, 0, GETDATE(), GETDATE())
END
GO

/*********** End 10.0.5 Changes *********************/

/*********** Begin 10.0.6 Changes *******************/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_MonthlyMaintenance]'))
	DROP PROC [dbo].[aspdnsf_MonthlyMaintenance]
GO

CREATE proc [dbo].[aspdnsf_MonthlyMaintenance]
	@purgeAnonCustomers tinyint = 1,
	@cleanShoppingCartsOlderThan smallint = 30, -- set to 0 to disable erasing
	@cleanWishListsOlderThan smallint = 30, -- set to 0 to disable erasing
	@eraseCCFromAddresses tinyint = 1, -- except those used for recurring billing items!
	@clearProductViewsOrderThan smallint = 180,
	@eraseCCFromOrdersOlderThan smallint = 30, -- set to 0 to disable erasing
	@defragIndexes tinyint = 0,
	@updateStats tinyint = 0,
	@purgeDeletedRecords tinyint = 0,-- Purges records in all tables with a deleted flag set to 1
	@removeRTShippingDataOlderThan smallint = 30, -- set to 0 to disable erasing
	@clearSearchLogOlderThan smallint = 30,-- set to 0 to disable erasing
	@cleanOrphanedLocalizedNames tinyint = 0,
	@cleanupSecurityLog tinyint = 0,
	@clearProfilesOlderThan smallint = 0
	as
begin
	set nocount on

	-- Clear out failed transactions older than 2 months:
	delete from FailedTransaction where OrderDate < dateadd(mm,-2,getdate());

	-- Clear out old tx info, not longer needed:
	update orders set TransactionCommand=NULL, AuthorizationResult=NULL, VoidTXCommand=NULL, VoidTXResult=NULL, CaptureTXCommand=NULL, CaptureTXResult=NULL, RefundTXCommand=NULL, RefundTXResult=NULL 
	where orderdate < dateadd(mm,-2,getdate());
	
	update dbo.Orders_ShoppingCart set
	ShippingDetail = ''
	where CreatedOn < dateadd(mm,-2,getdate());

	-- Clean up Security Log entries that are more than 1 year old
	if @cleanupSecurityLog = 1
	begin
		delete from SecurityLog where ActionDate < dateadd(year,-1,getdate());
	end

	-- Clean up data in the LocalizedObjectName table for locales that no longer exist
	if @cleanOrphanedLocalizedNames = 1
	begin
		delete from LocalizedObjectName where LocaleId not in (select LocaleSettingID from LocaleSetting);
	end

	-- clean up all carts (don't touch recurring items or wishlist items however):
	if @cleanShoppingCartsOlderThan <> 0
	begin
		delete dbo.kitcart where (CartType=0 or CartType=101) and CreatedOn < dateadd(d,-@cleanShoppingCartsOlderThan,getdate());
		delete dbo.ShoppingCart where (CartType=0 or CartType=101) and CreatedOn < dateadd(d,-@cleanShoppingCartsOlderThan,getdate());
	end

	if @cleanWishListsOlderThan <> 0
	begin
		delete dbo.kitcart where CartType=1 and CreatedOn < dateadd(d,-@cleanWishListsOlderThan,getdate());
		delete dbo.ShoppingCart where CartType=1 and CreatedOn < dateadd(d,-@cleanWishListsOlderThan,getdate());
	end

	-- purge anon customers:
	if @purgeAnonCustomers = 1
	begin
		-- clean out CIM profiles for orders that were not completed
		delete dbo.CIM_AddressPaymentProfileMap where customerid not in (select customerid from dbo.customer with (NOLOCK) where IsRegistered=1)

		delete dbo.customer where
			IsRegistered=0 and IsAdmin = 0
			and customerid not in (select customerid from dbo.ShoppingCart with (NOLOCK))
			and customerid not in (select customerid from dbo.kitcart with (NOLOCK))
			and customerid not in (select customerid from dbo.orders with (NOLOCK))
			and customerid not in (select customerid from dbo.rating with (NOLOCK))
			and customerid not in (select ratingcustomerid from dbo.ratingcommenthelpfulness with (NOLOCK))
			and customerid not in (select votingcustomerid from dbo.ratingcommenthelpfulness with (NOLOCK))
	end

	-- clean addresses, except for those that have recurring orders
	if @eraseCCFromAddresses = 1
		update [dbo].address set
			CardNumber=NULL,
			CardStartDate=NULL,
			CardIssueNumber=NULL,
			CardExpirationMonth=NULL,
			CardExpirationYear=NULL
		where CustomerID not in (select CustomerID from ShoppingCart where CartType=2)

	-- erase credit cards from all orders older than N days:
	if @eraseCCFromOrdersOlderThan <> 0
		update [dbo].orders set CardNumber=NULL
		where
			OrderDate < dateadd(d,-@eraseCCFromOrdersOlderThan,getdate())

	-- erase product views both for dynamic
	if @clearProductViewsOrderThan <> 0
	begin
		delete dbo.ProductView where ViewDate < dateadd(d,-@clearProductViewsOrderThan,getdate())
	end

	-- Nuke deleted stores
	declare @storeId int
	select top 1 @storeId = StoreID from Store where Deleted = 1
	while @@rowcount > 0 begin
		exec aspdnsf_NukeStore @storeId, 0
		select top 1 @storeId = StoreID from Store where Deleted = 1
	end

	if @purgeDeletedRecords = 1 begin
		delete dbo.Address where deleted = 1
		delete dbo.Coupon where deleted = 1
		delete dbo.Customer where deleted = 1
		delete dbo.Document where deleted = 1
		delete dbo.News where deleted = 1
		delete dbo.Product where deleted = 1
		delete dbo.ProductVariant where deleted = 1 or not exists (select * from dbo.Product where productid = ProductVariant.productid)
		delete dbo.SalesPrompt where deleted = 1
		delete dbo.ShippingZone where deleted = 1
		delete dbo.Topic where deleted = 1
		delete dbo.Affiliate where deleted = 1
		delete dbo.Category where deleted = 1
		delete dbo.CustomerLevel where deleted = 1
		delete dbo.Distributor where deleted = 1
		delete dbo.Genre where deleted = 1
		delete dbo.Library where deleted = 1
		delete dbo.Manufacturer where deleted = 1
		delete dbo.Section where deleted = 1
		delete dbo.Vector where deleted = 1
		delete dbo.ProductVector where not exists (select * from dbo.product where productid = ProductVector.productid) or not exists (select * from dbo.vector where vectorid = ProductVector.vectorid)
		delete dbo.ProductAffiliate where not exists (select * from dbo.product where productid = ProductAffiliate.productid) or not exists (select * from dbo.Affiliate where Affiliateid = ProductAffiliate.Affiliateid)
		delete dbo.ProductCategory where not exists (select * from dbo.product where productid = ProductCategory.productid) or not exists (select * from dbo.Category where Categoryid = ProductCategory.Categoryid)
		delete dbo.ProductCustomerLevel where not exists (select * from dbo.product where productid = ProductCustomerLevel.productid) or not exists (select * from dbo.CustomerLevel where CustomerLevelid = ProductCustomerLevel.CustomerLevelid)
		delete dbo.ProductDistributor where not exists (select * from dbo.product where productid = ProductDistributor.productid) or not exists (select * from dbo.Distributor where Distributorid = ProductDistributor.Distributorid)
		delete dbo.ProductGenre where not exists (select * from dbo.product where productid = ProductGenre.productid) or not exists (select * from dbo.Genre where Genreid = ProductGenre.Genreid)
		delete dbo.ProductLocaleSetting where not exists (select * from dbo.product where productid = ProductLocaleSetting.productid) or not exists (select * from dbo.LocaleSetting where LocaleSettingid = ProductLocaleSetting.LocaleSettingid)
		delete dbo.ProductManufacturer where not exists (select * from dbo.product where productid = ProductManufacturer.productid) or not exists (select * from dbo.Manufacturer where Manufacturerid = ProductManufacturer.Manufacturerid)
		delete dbo.ProductSection where not exists (select * from dbo.product where productid = ProductSection.productid) or not exists (select * from dbo.Section where Sectionid = ProductSection.Sectionid)
	end

	-- Clear out all customer sessions
	truncate table CustomerSession

	-- Clean up abandon records tied to customers that no longer exist
	delete from dbo.ShoppingCart where CustomerID not in (select distinct CustomerID from Customer);
	delete from dbo.KitCart where ShoppingCartRecID not in (select distinct ShoppingCartRecID from ShoppingCart);
	delete from dbo.CustomerSession where CustomerID not in (select distinct CustomerID from Customer);
	delete from dbo.RatingCommentHelpfulness where RatingCustomerID not in (select distinct CustomerID from Customer);
	delete from dbo.RatingCommentHelpfulness where VotingCustomerID not in (select distinct CustomerID from Customer);
	delete from dbo.PromotionUsage where CustomerID not in (select distinct CustomerID from Customer);
	delete from dbo.Address where CustomerID not in (select distinct CustomerID from Customer);
	delete from dbo.Rating where CustomerID not in (select distinct CustomerID from Customer);

	-- Remove old rtshipping requests and responses
	if @removeRTShippingDataOlderThan <> 0
	begin
		update dbo.Customer set RTShipRequest = '', RTShipResponse = ''
		where CreatedOn < dateadd(d,-@removeRTShippingDataOlderThan,getdate())

		update dbo.Orders set RTShipRequest = '', RTShipResponse = ''
		where OrderDate < dateadd(d,-@removeRTShippingDataOlderThan,getdate())
	end

	-- Search log
	if @clearSearchLogOlderThan <> 0
	begin
		delete from dbo.SearchLog where CreatedOn < dateadd(d,-@clearSearchLogOlderThan,getdate())
	end

	-- Profile table
	if @clearProfilesOlderThan != -1
	begin
		if @clearProfilesOlderThan = 0
		begin
			truncate table [dbo].[profile]
		end
		else
		begin
			delete from [dbo].[Profile] where UpdatedOn < dateadd(d,-@clearProfilesOlderThan,getdate());
		end
	end

	-- Defrag indexes
	DECLARE @indexesUpdated BIT = 0
	IF @defragIndexes = 1
	BEGIN
		DECLARE @cmd NVARCHAR(MAX), @tableName VARCHAR(128), @indexName VARCHAR(128)
		CREATE TABLE #INDEXESTOUPDATE(
			TableName VARCHAR(128),
			IndexName VARCHAR(128))

		INSERT INTO #IndexesToUpdate
		SELECT  o.name AS TableName, i.name AS IndexName
		FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, NULL) s
			JOIN sys.sysobjects o ON s.object_id = o.id
			LEFT JOIN sys.indexes i ON s.index_id = i.index_id AND o.id = i.object_id
		WHERE s.index_id != 0
			AND s.avg_fragmentation_in_percent > 10
			AND s.page_count > 500
		ORDER BY o.name

		DECLARE indexCursor CURSOR
			FOR SELECT TableName, IndexName FROM #IndexesToUpdate
		OPEN indexCursor
		FETCH NEXT FROM indexCursor
			INTO @tableName, @indexName
		WHILE @@FETCH_STATUS = 0
			BEGIN
				SET @cmd = 'ALTER INDEX [' + @indexName + '] ON [' + @tableName  + '] REBUILD'
				EXEC (@cmd)

			FETCH NEXT FROM indexCursor
				INTO @tableName, @indexName
			END
		CLOSE indexCursor
		DEALLOCATE indexCursor

		-- ALTER INDEX doesn't update statistics, have to trigger it manually
		EXEC [dbo].[aspdnsf_UpdateStatistics]
		SET @indexesUpdated = 1
	END

	--Update statistics, including ones that updating indexes misses
	IF @updateStats = 1 AND @indexesUpdated = 0 --This may have already been done above
	BEGIN
		EXEC [dbo].[aspdnsf_UpdateStatistics]
	END
end

GO

--Clarify that USPS web tools should only be used with USPS shipments
update AppConfig set [Description] = N'Leave blank if you do not want AspDotNetStorefront to verify addresses. Otherwise set to "USPS" and ensure the VerifyAddressesProvider.USPS server and userid are properly set. USPS Address Verification should only be used with USPS shipments.' where [Name] = N'VerifyAddressesProvider'
update AppConfig set [Description] = N'USPS userid for the Verify Address API. USPS Address Verification should only be used with USPS shipments.' where [Name] = N'VerifyAddressesProvider.USPS.UserID'
update AppConfig set [Description] = N'Set to true to enable city/state lookup service based on postal code. USPS Postal Code lookup should only be used with USPS shipments.' where [Name] = N'Address.UsePostalCodeLookupService'
update AppConfig set [Description] = N'Your user ID for USPS Web Tools. USPS Postal Code lookup should only be used with USPS shipments. This is provided by USPS when you register at https://www.usps.com/business/web-tools-apis/welcome.htm' where [Name] = N'Address.PostalCodeLookupService.UspsUserId'

--Adding Sage Pay PI
IF NOT EXISTS (SELECT * FROM sysobjects WHERE id = OBJECT_ID('CurrencyExceptions') AND TYPE = 'u')
BEGIN
	CREATE TABLE [dbo].[CurrencyExceptions] (
		[ID]                    INT              IDENTITY (1, 1) NOT NULL,
		[CurrencyCode]          NVARCHAR(10)     NULL,
		[AmountToSmallestUnit]  INT              NOT NULL,
		[CreatedOn]             DATETIME         NOT NULL,
		[UpdatedOn]             DATETIME         NOT NULL
	);
END

--Insert into CurrencyExceptions
GO

IF NOT EXISTS(SELECT CurrencyCode FROM CurrencyExceptions WHERE CurrencyCode = 'VND')
BEGIN
	INSERT INTO [dbo].[CurrencyExceptions] ([CurrencyCode], [AmountToSmallestUnit], [UpdatedOn], [CreatedOn]) VALUES ('VND', 10, GETDATE(), GETDATE())
END

GO

IF NOT EXISTS(SELECT CurrencyCode FROM CurrencyExceptions WHERE CurrencyCode = 'TND')
BEGIN
	INSERT INTO [dbo].[CurrencyExceptions] ([CurrencyCode], [AmountToSmallestUnit], [UpdatedOn], [CreatedOn]) VALUES ('TND', 1000, GETDATE(), GETDATE())
END

GO

IF NOT EXISTS(SELECT CurrencyCode FROM CurrencyExceptions WHERE CurrencyCode = 'OMR')
BEGIN
	INSERT INTO [dbo].[CurrencyExceptions] ([CurrencyCode], [AmountToSmallestUnit], [UpdatedOn], [CreatedOn]) VALUES ('OMR', 1000, GETDATE(), GETDATE())
END

GO

IF NOT EXISTS(SELECT CurrencyCode FROM CurrencyExceptions WHERE CurrencyCode = 'MRO')
BEGIN
	INSERT INTO [dbo].[CurrencyExceptions] ([CurrencyCode], [AmountToSmallestUnit], [UpdatedOn], [CreatedOn]) VALUES ('MRO', 5, GETDATE(), GETDATE())
END

GO

IF NOT EXISTS(SELECT CurrencyCode FROM CurrencyExceptions WHERE CurrencyCode = 'MGA')
BEGIN
	INSERT INTO [dbo].[CurrencyExceptions] ([CurrencyCode], [AmountToSmallestUnit], [UpdatedOn], [CreatedOn]) VALUES ('MGA', 5, GETDATE(), GETDATE())
END

GO

IF NOT EXISTS(SELECT CurrencyCode FROM CurrencyExceptions WHERE CurrencyCode = 'LYD')
BEGIN
	INSERT INTO [dbo].[CurrencyExceptions] ([CurrencyCode], [AmountToSmallestUnit], [UpdatedOn], [CreatedOn]) VALUES ('LYD', 1000, GETDATE(), GETDATE())
END

GO

IF NOT EXISTS(SELECT CurrencyCode FROM CurrencyExceptions WHERE CurrencyCode = 'KWD')
BEGIN
	INSERT INTO [dbo].[CurrencyExceptions] ([CurrencyCode], [AmountToSmallestUnit], [UpdatedOn], [CreatedOn]) VALUES ('KWD', 1000, GETDATE(), GETDATE())
END

GO

IF NOT EXISTS(SELECT CurrencyCode FROM CurrencyExceptions WHERE CurrencyCode = 'IQD')
BEGIN
	INSERT INTO [dbo].[CurrencyExceptions] ([CurrencyCode], [AmountToSmallestUnit], [UpdatedOn], [CreatedOn]) VALUES ('IQD', 1000, GETDATE(), GETDATE())
END

GO

IF NOT EXISTS(SELECT CurrencyCode FROM CurrencyExceptions WHERE CurrencyCode = 'BHD')
BEGIN
	INSERT INTO [dbo].[CurrencyExceptions] ([CurrencyCode], [AmountToSmallestUnit], [UpdatedOn], [CreatedOn]) VALUES ('BHD', 1000, GETDATE(), GETDATE())
END

--Insert into AppConfig
GO

IF NOT EXISTS (SELECT * FROM AppConfig WHERE Name = 'SagePayPi.VendorName')
BEGIN
	INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn]) VALUES (0, N'SagePayPi.VendorName', N'The VendorName given to you by Sage Pay PI.', N'', N'string', NULL, N'GATEWAY', 0, 0, GETDATE(), GETDATE())
END

GO
	
IF NOT EXISTS (SELECT * FROM AppConfig WHERE Name = 'SagePayPi.IntegrationKey')
BEGIN
	INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn]) VALUES (0, N'SagePayPi.IntegrationKey', N'The Integration Key given to you by Sage Pay PI.', N'', N'string', NULL, N'GATEWAY', 0, 0, GETDATE(), GETDATE())
END

GO
	
IF NOT EXISTS (SELECT * FROM AppConfig WHERE Name = 'SagePayPi.IntegrationPassword')
BEGIN
	INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn]) VALUES (0, N'SagePayPi.IntegrationPassword', N'The Integration Password given to you by Sage Pay PI.', N'', N'string', NULL, N'GATEWAY', 0, 0, GETDATE(), GETDATE())
END

GO	
	
IF NOT EXISTS (SELECT * FROM AppConfig WHERE Name = 'SagePayPi.3dSecureEnabled')
BEGIN
	INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn]) VALUES (0, N'SagePayPi.3dSecureEnabled', N'If true, customers checking out with credit cards through Sage Pay PI will be shown the 3dSecure form if their card is enrolled. 3dSecure must be enabled on the Sage Pay PI account, which requires help from their Support department.', N'false', N'boolean', NULL, N'GATEWAY', 0, 0, GETDATE(), GETDATE())
END

GO

IF NOT EXISTS (SELECT * FROM AppConfig WHERE Name = 'SagePayPi.LiveScriptUrl')
BEGIN
	INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn]) VALUES (0, N'SagePayPi.LiveScriptUrl', N'URL to the live javascript file. Do not modify.', N'https://pi-live.sagepay.com/api/v1/js/sagepay.js', N'string', NULL, N'GATEWAY', 0, 0, GETDATE(), GETDATE())
END

GO
	
IF NOT EXISTS (SELECT * FROM AppConfig WHERE Name = 'SagePayPi.TestScriptUrl')
BEGIN
	INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn]) VALUES (0, N'SagePayPi.TestScriptUrl', N'URL to the test javascript file. Do not modify.', N'https://pi-test.sagepay.com/api/v1/js/sagepay.js', N'string', NULL, N'GATEWAY', 0, 0, GETDATE(), GETDATE())
END

GO
	
IF NOT EXISTS (SELECT * FROM AppConfig WHERE Name = 'SagePayPi.LiveUrl')
BEGIN
	INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn]) VALUES (0, N'SagePayPi.LiveUrl', N'URL to the Sage Pay PI live API. Do not modify.', N'https://pi-live.sagepay.com/api/v1/', N'string', NULL, N'GATEWAY', 0, 0, GETDATE(), GETDATE())
END

GO
	
IF NOT EXISTS (SELECT * FROM AppConfig WHERE Name = 'SagePayPi.TestUrl')
BEGIN
	INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn]) VALUES (0, N'SagePayPi.TestUrl', N'URL to the Sage Pay PI test API. Do not modify.', N'https://pi-test.sagepay.com/api/v1/', N'string', NULL, N'GATEWAY', 0, 0, GETDATE(), GETDATE())
END

GO
	
IF NOT EXISTS (SELECT * FROM AppConfig WHERE Name = 'SagePayPi.3DSecureTermUrl')
BEGIN
	INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn]) VALUES (0, N'SagePayPi.3DSecureTermUrl', N'URL which points to a page on your site to which the bank will return the customer.', N'/threedsecure/sagepaypipares', N'string', NULL, N'GATEWAY', 0, 0, GETDATE(), GETDATE())
END

GO

IF NOT EXISTS (SELECT * FROM AppConfig WHERE Name = 'SagePayPI.CustomerFriendlyErrors')
BEGIN
	INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn]) VALUES (0, N'SagePayPI.CustomerFriendlyErrors', N'If true, customer friendly errors will display to the user, otherwise more specific API errors will display which are useful for troubleshooting.', N'true', N'boolean', NULL, N'GATEWAY', 0, 0, GETDATE(), GETDATE())
END

GO
--Sage Pay PI Complete

/*********** End 10.0.6 Changes *********************/

/*********** Begin 10.0.7 Changes *******************/

IF NOT EXISTS (SELECT * FROM Topic WHERE Name = 'CheckoutSecuritySeal')
BEGIN
	INSERT INTO [dbo].[Topic] ([Name], [Title], [Description], [SETitle], [SEDescription], [SEKeywords], [Password], [RequiresDisclaimer], [XmlPackage], [ExtensionData], [ShowInSiteMap], [SkinID], [HTMLOk], [Deleted], [StoreID], [DisplayOrder], [CreatedOn], [Published], [UpdatedOn], [IsFrequent]) 
	VALUES (N'CheckoutSecuritySeal', N'Checkout Security Seal', N'<div style="text-align:center;">
	<em class="fa fa-shield" style="font-size:32px;vertical-align:top;" aria-hidden="true"></em>
	<span style="vertical-align: middle; font-size: 18px;">Secured with SSL</span></div>', NULL, NULL, NULL, NULL, 0, NULL, NULL, 0, 0, 1, 0, 0, 1, GETDATE(), 1, GETDATE(), 1)
END

IF NOT EXISTS (SELECT * FROM AppConfig WHERE Name = 'PhoneNumberMask.Enabled')
BEGIN
	INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn]) VALUES (0, N'PhoneNumberMask.Enabled', N'If true, customer-entered phone numbers will be forced to adhere to a standard format, for example "(000) 000-0000".  Currently this only affects United States phone numbers.', N'true', N'boolean', NULL, N'CHECKOUT', 0, 0, GETDATE(), GETDATE())
END

--Remove SagePay UK Simulator support
delete from AppConfig where [Name] in ('SagePayUK.UseSimulator', 'SagePayUKURL.Simulator.Abort', 'SagePayUKURL.Simulator.Callback', 'SagePayUKURL.Simulator.Purchase', 'SagePayUKURL.Simulator.Refund', 'SagePayUKURL.Simulator.Release')

-- Contact Us emails/names
if not exists(select Name from AppConfig where Name = 'ContactUsFromEmail')
begin
	insert [dbo].AppConfig(SuperOnly, Name, GroupName, Description, ValueType, ConfigValue) 
	values(0,'ContactUsFromEmail', 'EMAIL', 'The e-mail address from which you want your store Contact Us e-mails sent.', 'string', (select ConfigValue from AppConfig where Name = 'GotOrderEMailFrom' and StoreId = 0))

	insert into dbo.AppConfig(SuperOnly, Name, GroupName, Description, ValueType, ConfigValue, StoreID)
	select 0, 'ContactUsFromEmail', 'EMAIL', 'The e-mail address from which you want your store Contact Us e-mails sent.', 'string', ConfigValue, StoreID
	from AppConfig 
	where 
		Name = 'GotOrderEMailFrom'
		and StoreID > 0
		and not exists(select * from AppConfig where Name = 'ContactUsFromEmail' and StoreID = StoreID)
end

if not exists(select Name from AppConfig where Name = 'ContactUsFromName')
begin
	insert [dbo].AppConfig(SuperOnly, Name, GroupName, Description, ValueType, ConfigValue) 
	values(0,'ContactUsFromName', 'EMAIL', 'The name from which your Contact Us emails are sent, for example, Sally Jane.', 'string', (select ConfigValue from AppConfig where Name = 'GotOrderEMailFromName' and StoreId = 0))

	insert into dbo.AppConfig(SuperOnly, Name, GroupName, Description, ValueType, ConfigValue, StoreID)
	select 0, 'ContactUsFromName', 'EMAIL', 'The name from which your Contact Us emails are sent, for example, Sally Jane.', 'string', ConfigValue, StoreID
	from AppConfig 
		where Name = 'GotOrderEMailFromName'
		and StoreID > 0
		and not exists(select * from AppConfig where Name = 'ContactUsFromName' and StoreID = StoreID)
end

if not exists(select Name from AppConfig where Name = 'ContactUsToEmail')
begin
	insert [dbo].AppConfig(SuperOnly, Name, GroupName, Description, ValueType, ConfigValue) 
	values(0,'ContactUsToEmail', 'EMAIL', 'The e-mail address to which you want your store Contact Us e-mails sent.', 'string', (select ConfigValue from AppConfig where Name = 'GotOrderEMailTo' and StoreId = 0))

	insert into dbo.AppConfig(SuperOnly, Name, GroupName, Description, ValueType, ConfigValue, StoreID)
	select 0, 'ContactUsToEmail', 'EMAIL', 'The e-mail address to which you want your store Contact Us e-mails sent.', 'string', ConfigValue, StoreID
	from AppConfig 
		where Name = 'GotOrderEMailTo'
		and StoreID > 0
		and not exists(select * from AppConfig where Name = 'ContactUsToEmail' and StoreID = StoreID)
end

if not exists(select Name from AppConfig where Name = 'ContactUsToName')
begin
	insert [dbo].AppConfig(SuperOnly, Name, GroupName, Description, ValueType, ConfigValue) 
	values(0,'ContactUsToName', 'EMAIL', 'The name to which your Contact Us emails are sent, for example, Sally Jane.', 'string', (select ConfigValue from AppConfig where Name = 'GotOrderEMailTo' and StoreId = 0))

	insert into dbo.AppConfig(SuperOnly, Name, GroupName, Description, ValueType, ConfigValue, StoreID)
	select 0, 'ContactUsToName', 'EMAIL', 'The name to which your Contact Us emails are sent, for example, Sally Jane.', 'string', ConfigValue, StoreID
	from AppConfig 
		where Name = 'GotOrderEMailTo'
		and StoreID > 0
		and not exists(select * from AppConfig where Name = 'ContactUsToName' and StoreID = StoreID)
end

-- Signifyd Fraud Protection
if not exists(select Name from AppConfig where Name = 'Signifyd.Enabled')
	insert [dbo].AppConfig(SuperOnly,Name,GroupName,Description,ValueType,ConfigValue)
	values(0,'Signifyd.Enabled','GATEWAY','If Yes, the Signifyd fraud protection service will be used before a credit card is captured by the gateway. See signifyd.com for more information.','boolean','false')

if not exists(select Name from AppConfig where Name = 'Signifyd.API.Url')
	insert [dbo].AppConfig(SuperOnly,Name,GroupName,Description,ValueType,ConfigValue)
	values(0,'Signifyd.API.Url','GATEWAY','The API endpoint address for Signifyd Fraud Protection.','string','https://api.signifyd.com/v2')

if not exists(select Name from AppConfig where Name = 'Signifyd.Console.Url')
	insert [dbo].AppConfig(SuperOnly,Name,GroupName,Description,ValueType,ConfigValue)
	values(0,'Signifyd.Console.Url','GATEWAY','The management site address for Signifyd Fraud Protection.','string','https://app.signifyd.com')

if not exists(select Name from AppConfig where Name = 'Signifyd.Team.Key')
	insert [dbo].AppConfig(SuperOnly,Name,GroupName,Description,ValueType,ConfigValue)
	values(0,'Signifyd.Team.Key','GATEWAY','The API authentication key used by Signifyd API calls.','string','')

if not exists(select Name from AppConfig where Name = 'Signifyd.DeclineAction.Void')
	insert [dbo].AppConfig(SuperOnly,Name,GroupName,Description,ValueType,ConfigValue)
	values(0,'Signifyd.DeclineAction.Void','GATEWAY','If Yes, when Signifyd declines an order, then the order will be voided. Otherwise, do nothing and let the Admin manually change the order status.','boolean','true')

if not exists(select Name from AppConfig where Name = 'Signifyd.Log.Enabled')
	insert [dbo].AppConfig(SuperOnly,Name,GroupName,Description,ValueType,ConfigValue)
	values(0,'Signifyd.Log.Enabled','GATEWAY','If Yes, then Signifyd activity is logged to the system log.','boolean','false')

if not exists (select * from sys.tables where name='SignifydOrderStatus')
begin
	create table [dbo].[SignifydOrderStatus](
		[SignifydOrderStatusID] int identity(1,1) not null,
		[OrderNumber] int not null,
		[GuaranteedStatus] nvarchar(max) not null,
		[InvestigationID] int not null
	constraint [PK_SignifydOrderStatus] primary key clustered ([SignifydOrderStatusID] asc))
end
GO

if not exists(select Name from AppConfig where Name = 'Address.CollectSuite')
begin
	insert [dbo].AppConfig(SuperOnly, Name, GroupName, Description, ValueType, ConfigValue) 
	values(0,'Address.CollectSuite', 'DISPLAY', 'If true, address forms include the suite field.', N'boolean', N'true')
end

/*********** End 10.0.7 Changes *********************/

/*********** Begin 10.0.8 Changes *******************/

IF NOT EXISTS (SELECT * FROM AppConfig WHERE Name = 'eWAY.APIKey')
BEGIN
	INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn]) VALUES (0, N'eWAY.APIKey', N'Your API key for eWAY which is in MYeWAY in My Account under API Key.', N'', N'string', NULL, N'GATEWAY', 0, 0, GETDATE(), GETDATE())
END

IF NOT EXISTS (SELECT * FROM AppConfig WHERE Name = 'eWAY.APIPassword')
BEGIN
	INSERT INTO [dbo].[AppConfig] ([StoreID], [Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden], [CreatedOn], [UpdatedOn]) VALUES (0, N'eWAY.APIPassword', N'Your API password for eWAY which is in MYeWAY in My Account under API Key.  This is not your password for the eWAY Partner Portal.', N'', N'string', NULL, N'GATEWAY', 0, 0, GETDATE(), GETDATE())
END

/*********** End 10.0.8 Changes *********************/

/*********** Begin 10.0.10 Changes *******************/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_GetAvailablePromos]'))
	DROP PROC [dbo].[aspdnsf_GetAvailablePromos]
GO

CREATE PROC [dbo].[aspdnsf_GetAvailablePromos]
    @productIdList	nvarchar(max),
	@StoreID		int = 0,
	@CustomerID		int = 0,
	@CustomerLevel	int = 0

AS
BEGIN
	declare @productIds table (ProductId int not null)
	insert into @productIds select distinct * from dbo.Split(@productIdList, ',')

	declare @FilterPromotions tinyint
	SET @FilterPromotions = (SELECT case ConfigValue WHEN 'true' THEN 1 ELSE 0 END FROM GlobalConfig WHERE Name='AllowPromotionFiltering')

	DECLARE @CustomerEmail varchar(max)
	SELECT @CustomerEmail = Email FROM Customer WHERE CustomerID = @CustomerID

	select
		DISTINCT ids.ProductId,
		p.CallToAction
	from
		Promotions p
			left join (SELECT PromotionRuleData.value('(/ArrayOfPromotionRuleBase/PromotionRuleBase/ExpirationDate/node())[1]', 'nvarchar(40)') AS ExpDate, Id
				FROM Promotions) AS e on e.Id = p.Id
			left join (SELECT PromotionRuleData.value('(/ArrayOfPromotionRuleBase/PromotionRuleBase/StartDate/node())[1]', 'nvarchar(40)') AS StartDate, Id
				FROM Promotions) AS s on s.Id = p.Id
			left join PromotionStore pt
				on p.Id = pt.PromotionID, @productIds ids
			left join ProductCategory pc
				on pc.ProductId = ids.ProductId
			left join ProductSection ps
				on ps.ProductId = ids.ProductId
			left join ProductManufacturer pm
				on pm.ProductId = ids.ProductId
	where
		(e.ExpDate IS NULL OR CONVERT(date, e.ExpDate) > GETDATE())
		and (s.StartDate IS NULL OR CONVERT(date, s.StartDate) < GETDATE())
		and p.Active = 1
		and p.AutoAssigned = 1
		and isnull(p.CallToAction, '') != ''
		and (@FilterPromotions = 0 OR pt.StoreID = @StoreID)
		and (
				-- ProductIdPromotionRule
				p.PromotionRuleData.exist('/ArrayOfPromotionRuleBase/PromotionRuleBase/ProductIds[int = sql:column("ids.ProductId")]') = 1

				-- CategoryPromotionRule
				or p.PromotionRuleData.exist('/ArrayOfPromotionRuleBase/PromotionRuleBase/CategoryIds[int = sql:column("pc.CategoryId")]') = 1

				-- SectionPromotionRule
				or p.PromotionRuleData.exist('/ArrayOfPromotionRuleBase/PromotionRuleBase/SectionIds[int = sql:column("ps.SectionId")]') = 1

				-- ManufacturerPromotionRule
				or p.PromotionRuleData.exist('/ArrayOfPromotionRuleBase/PromotionRuleBase/ManufacturerIds[int = sql:column("pm.ManufacturerId")]') = 1

				-- GiftProductPromotionDiscount
				or p.PromotionDiscountData.exist('/ArrayOfPromotionDiscountBase/PromotionDiscountBase/GiftProductIds[int = sql:column("ids.ProductId")]') = 1
			)
		-- Email Address rule
		and (
				p.PromotionRuleData.exist('/ArrayOfPromotionRuleBase/PromotionRuleBase/EmailAddresses') = 0
				or p.PromotionRuleData.exist('/ArrayOfPromotionRuleBase/PromotionRuleBase/EmailAddresses[string = sql:variable("@CustomerEmail")]') = 1
			)
		-- Customer Level rule
		and (
				p.PromotionRuleData.exist('/ArrayOfPromotionRuleBase/PromotionRuleBase/CustomerLevels') = 0
				or p.PromotionRuleData.exist('/ArrayOfPromotionRuleBase/PromotionRuleBase/CustomerLevels[int = sql:variable("@CustomerLevel")]') = 1
			)
	for xml path('Promotion')
END
GO

--Fix default data abbreviation for Yukon Territory
UPDATE [State] SET Abbreviation = N'YT' WHERE Name = N'Yukon Territory'

-- Add Accept.js
IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Live.ApiLoginId')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'AcceptJs.Live.ApiLoginId', 
		'Live Accept.js API login ID provided by A.Net', 
		'string', 
		NULL, 
		'', 
		'Gateway', 0, 0);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Test.ApiLoginId')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'AcceptJs.Test.ApiLoginId', 
		'Test Accept.js API login ID provided by A.Net', 
		'string', 
		NULL, 
		'', 
		'Gateway', 0, 0);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Live.TransactionKey')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'AcceptJs.Live.TransactionKey', 
		'Live Accept.js transaction key provided by A.Net', 
		'string', 
		NULL, 
		'', 
		'Gateway', 0, 0);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Test.TransactionKey')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'AcceptJs.Test.TransactionKey', 
		'Test Accept.js transaction key provided by A.Net', 
		'string', 
		NULL, 
		'', 
		'Gateway', 0, 0);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Live.ClientKey')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'AcceptJs.Live.ClientKey', 
		'Live Accept.js client key provided by A.Net', 
		'string', 
		NULL, 
		'', 
		'Gateway', 0, 0);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Test.ClientKey')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'AcceptJs.Test.ClientKey', 
		'Test Accept.js client key provided by A.Net', 
		'string', 
		NULL, 
		'', 
		'Gateway', 0, 0);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Live.Url')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'AcceptJs.Live.Url', 
		'A.Net live Accept.js URL.  Do not change this unless told to by support!', 
		'string', 
		NULL, 
		'//js.authorize.net/v3/AcceptUI.js', 
		'Gateway', 0, 0);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Test.Url')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'AcceptJs.Test.Url', 
		'A.Net test Accept.js URL.  Do not change this unless told to by support!', 
		'string', 
		NULL, 
		'//jstest.authorize.net/v3/AcceptUI.js', 
		'Gateway', 0, 0);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[Topic] WHERE [Name] = 'AcceptJsCCFormInfo')
BEGIN
INSERT INTO Topic (Name, Title, [Description], ShowInSiteMap) VALUES
(
	'AcceptJsCCFormInfo', 
	'AcceptJsCCFormInfo', 
	'
	Clicking the button below will open a payment form for you to securely add your credit card details.
	',
	0
);
END
GO

/*********** End 10.0.10 Changes *********************/

/*********** Begin 10.0.11 Changes *******************/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_updAddress]'))
	DROP PROC [dbo].[aspdnsf_updAddress]
GO

CREATE PROCEDURE [dbo].[aspdnsf_updAddress]
(
	@AddressID int,
	@NickName nvarchar(100) = null,
	@FirstName nvarchar(100) = null,
	@LastName nvarchar(100) = null,
	@Company nvarchar(100) = null,
	@Address1 nvarchar(100) = null,
	@Address2 nvarchar(100) = null,
	@Suite nvarchar(50) = null,
	@City nvarchar(100) = null,
	@State nvarchar(100) = null,
	@Zip nvarchar(100) = null,
	@Country nvarchar(100) = null,
	@ResidenceType int = null,
	@Phone nvarchar(25) = null,
	@Email nvarchar(100) = null
)
AS
BEGIN
SET NOCOUNT ON
UPDATE [Address]
SET
	[NickName]		= COALESCE(@NickName, [NickName]),
	[FirstName]		= COALESCE(@FirstName, [FirstName]),
	[LastName]		= COALESCE(@LastName, [LastName]),
	[Company]		= COALESCE(@Company, [Company]),
	[Address1]		= COALESCE(@Address1, [Address1]),
	[Address2]		= COALESCE(@Address2, [Address2]),
	[Suite]			= COALESCE(@Suite, [Suite]),
	[City]			= COALESCE(@City, [City]),
	[State]			= COALESCE(@State, [State]),
	[Zip]			= COALESCE(@Zip, [Zip]),
	[Country]		= COALESCE(@Country, [Country]),
	[ResidenceType]	= COALESCE(@ResidenceType, [ResidenceType]),
	[Phone]			= COALESCE(@Phone, [Phone]),
	[Email]			= COALESCE(@Email, [Email])
WHERE [AddressID] = @AddressID
END
GO

/*********** End 10.0.11 Changes *********************/

/*********** Begin 10.0.13 Changes *******************/
--Add eCheck
UPDATE dbo.[AppConfig]
SET AllowableValues = rtrim(ltrim(AllowableValues)) + ',eCheck'
WHERE [Name] = 'PaymentMethods'
AND AllowableValues NOT LIKE '%echeck%'

IF NOT EXISTS(SELECT * FROM [dbo].[Topic] WHERE [Name] = 'AcceptJsECheckFormInfo')
BEGIN
INSERT INTO Topic (Name, Title, [Description], ShowInSiteMap) VALUES
(
	'AcceptJsECheckFormInfo', 
	'AcceptJsECheckFormInfo', 
	'Please enter your eCheck details.  Please note that shipping may be delayed for eCheck processing.',
	0
);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[Topic] WHERE [Name] = 'AcceptJsECheckFormAuthorizationText')
BEGIN
INSERT INTO Topic (Name, Title, [Description], ShowInSiteMap) VALUES
(
	'AcceptJsECheckFormAuthorizationText', 
	'AcceptJsECheckFormAuthorizationText', 
	'By clicking the button below, I authorize (!AppConfig Name=''storename''!) to charge this account for the checkout amount.',
	0
);
END
GO

IF EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Live.Url')
BEGIN
	DELETE FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Live.Url'
END
GO

IF EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Test.Url')
BEGIN
	DELETE FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Test.Url'
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Form.Hosted.Live.Url')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'AcceptJs.Form.Hosted.Live.Url', 
		'A.Net live Accept.js URL for A.Net hosted form.  Do not change this unless told to by support!', 
		'string', 
		NULL, 
		'https://js.authorize.net/v3/AcceptUI.js', 
		'Gateway', 0, 0);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Form.Hosted.Test.Url')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'AcceptJs.Form.Hosted.Test.Url', 
		'A.Net test Accept.js URL for A.Net hosted form.  Do not change this unless told to by support!', 
		'string', 
		NULL, 
		'https://jstest.authorize.net/v3/AcceptUI.js', 
		'Gateway', 0, 0);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Form.Own.Live.Url')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'AcceptJs.Form.Own.Live.Url', 
		'A.Net live Accept.js URL.  Do not change this unless told to by support!', 
		'string', 
		NULL, 
		'https://js.authorize.net/v1/Accept.js', 
		'Gateway', 0, 0);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'AcceptJs.Form.Own.Test.Url')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'AcceptJs.Form.Own.Test.Url', 
		'A.Net test Accept.js URL.  Do not change this unless told to by support!', 
		'string', 
		NULL, 
		'https://jstest.authorize.net/v1/Accept.js', 
		'Gateway', 0, 0);
END
GO

/*********** End 10.0.13 Changes *******************/

/*********** Begin 10.0.14 Changes *******************/

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'Shipping.Hide.Options')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'Shipping.Hide.Options', 
		'If true, the shipping options section will collapse after a shipping method is selected.', 
		'boolean', 
		NULL, 
		'true', 
		'SHIPPING', 0, 0);
END
GO

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_GetShoppingCart]'))
	DROP PROC [dbo].[aspdnsf_GetShoppingCart]
GO

CREATE PROC [dbo].[aspdnsf_GetShoppingCart]
    @CartType tinyint, -- ShoppingCart = 0, WishCart = 1, RecurringCart = 2
    @CustomerID int,
    @OriginalRecurringOrderNumber int,
    @OnlyLoadRecurringItemsThatAreDue tinyint,
    @StoreID int = 1

AS
BEGIN
    SET NOCOUNT ON
    declare @FilterShoppingCart bit, @FilterProduct bit
	SELECT TOP 1 @FilterShoppingCart = ConfigValue FROM GlobalConfig WHERE Name='AllowShoppingcartFiltering'
    SELECT TOP 1 @FilterProduct = ConfigValue FROM GlobalConfig WHERE Name='AllowProductFiltering'

    SELECT
        ShoppingCart.ProductSKU,
        ShoppingCart.IsUpsell,
        ShoppingCart.Notes,
        ShoppingCart.ExtensionData,
        ShoppingCart.CustomerEntersPrice,
        ShoppingCart.NextRecurringShipDate,
        ShoppingCart.RecurringIndex,
        ShoppingCart.OriginalRecurringOrderNumber,
        ShoppingCart.RecurringSubscriptionID,
        ShoppingCart.CartType,
        ShoppingCart.ProductPrice,
        ShoppingCart.ProductWeight,
        ShoppingCart.ProductDimensions,
        ShoppingCart.ShoppingCartRecID,
        ShoppingCart.ProductID,
        ShoppingCart.VariantID,
        ShoppingCart.Quantity,
        ShoppingCart.IsTaxable,
        ShoppingCart.TaxClassID,
        ShoppingCart.TaxRate,
        ShoppingCart.IsShipSeparately,
        ShoppingCart.ChosenColor,
        ShoppingCart.ChosenColorSKUModifier,
        ShoppingCart.ChosenSize,
        ShoppingCart.ChosenSizeSKUModifier,
        ShoppingCart.TextOption,
        ShoppingCart.IsDownload,
        ShoppingCart.FreeShipping,
        ShoppingCart.DistributorID,
        ShoppingCart.DownloadLocation,
        ShoppingCart.CreatedOn,
        ShoppingCart.BillingAddressID as ShoppingCartBillingAddressID,
        ShoppingCart.ShippingAddressID as ShoppingCartShippingAddressID,
        ShoppingCart.ShippingMethodID,
        ShoppingCart.ShippingMethod,
        ShoppingCart.RequiresCount,
        ShoppingCart.IsSystem,
        ShoppingCart.IsAKit,
        ShoppingCart.IsGift,
        Customer.EMail,
        Customer.OrderOptions,
        Customer.OrderNotes,
        Customer.FinalizationData,
        Customer.CouponCode,
        Customer.ShippingAddressID as
        CustomerShippingAddressID,
        Customer.BillingAddressID as CustomerBillingAddressID,
        Product.Name as ProductName,
        Product.IsSystem,
        ProductVariant.name as VariantName,
        Product.TextOptionPrompt,
        Product.SizeOptionPrompt,
        Product.ColorOptionPrompt,
        ProductVariant.CustomerEntersPricePrompt,
        Product.ProductTypeId,
        Product.TaxClassId,
        Product.ManufacturerPartNumber,
        Product.ImageFilenameOverride,
        Product.SEName,
        Product.SEAltText,
        Product.Deleted,
        ProductVariant.Weight,
        case @CartType when 2 then ShoppingCart.RecurringInterval else productvariant.RecurringInterval end RecurringInterval,
        case @CartType when 2 then ShoppingCart.RecurringIntervalType else productvariant.RecurringIntervalType end RecurringIntervalType

    FROM dbo.Customer with (NOLOCK)
        join dbo.ShoppingCart with (NOLOCK) ON Customer.CustomerID = ShoppingCart.CustomerID
        join dbo.Product with (NOLOCK) on ShoppingCart.ProductID=Product.ProductID
        left join dbo.ProductVariant with (NOLOCK) on ShoppingCart.VariantID=ProductVariant.VariantID
        left join dbo.Address with (NOLOCK) on Customer.ShippingAddressID=Address.AddressID
		inner join (select distinct a.ProductID,a.StoreID from ShoppingCart a with (nolock) left join ProductStore b with (nolock) on a.ProductID = b.ProductID and (@FilterProduct = 0 or a.StoreID = b.StoreID)) productstore
        on ShoppingCart.ProductID = productstore.ProductID and ShoppingCart.StoreID = productstore.StoreID

    WHERE ShoppingCart.CartType = @CartType
        and Product.Deleted in (0,2)
        and ProductVariant.Deleted = 0
        and Customer.customerid = @CustomerID
        and (@OriginalRecurringOrderNumber = 0 or ShoppingCart.OriginalRecurringOrderNumber = @OriginalRecurringOrderNumber)
        and (@OnlyLoadRecurringItemsThatAreDue = 0 or (@CartType = 2 and NextRecurringShipDate < dateadd(dy, 1, getdate())))
        and (@FilterShoppingCart = 0 or ShoppingCart.StoreID = @StoreID)
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[Topic] WHERE [Name] = 'HomeTopIntro')
BEGIN
INSERT INTO Topic (Name, Title, [Description], ShowInSiteMap) VALUES 
(
	'HomeTopIntro', 
	'Home Page Contents', 
	'(!TOPIC Name="HomePage.HomeImage"!)
	<div class="row">
		<div class="col-md-4">
			<div class="home-image">
				<img src="(!SkinPath!)/images/home1.jpg" alt="Giftcards" class="img-responsive center-block" />
			</div>
		</div>
		<div class="col-md-4">
			<div class="home-image">
				<img src="(!SkinPath!)/images/home2.jpg" alt="Storefronts" class="img-responsive center-block" />
			</div>
		</div>
		<div class="col-md-4">
			<div class="home-image">
				<img src="(!SkinPath!)/images/home3.jpg" alt="Documents" class="img-responsive center-block" />
			</div>
		</div>
	</div>
	<div class="row">
		<div class="col-md-6">
			<p>Your AspDotNetStorefront store is a thing of beauty right out of the box, but it''s almost certain that you''re going to want to stamp your own brand identity onto the ''skin''.</p>
			<h2 class="group-header">Three ways to personalize your store design</h2>
			<div class="row">
				<div class="col-md-4">
					<a href="http://www.aspdotnetstorefront.com/linkmanager.aspx?topic=10000skin&amp;type=adminskinning" class="thumbnail" target="_blank">
						<span class="thumbnail-items text-center">
							<img alt="Computer depicting that it can be used when editing the admin wizard" src="(!SkinPath!)/images/box1.jpg" class="img-responsive center-block" />
						</span>
						<span class="thumbnail-items text-center">
							Use the provided admin wizard
						</span>
					</a>
				</div>
				<div class="col-md-4">
					<a href="http://www.aspdotnetstorefront.com/linkmanager.aspx?topic=10000skin&amp;type=learntoskin" class="thumbnail" target="_blank">
						<span class="thumbnail-items text-center">
							<img alt="Book depicting instructions on learning how to skin a store" src="(!SkinPath!)/images/box2.jpg" class="img-responsive center-block" />
						</span>
						<span class="thumbnail-items text-center">
							Learn how to ''skin'' a store
						</span>
					</a>
				</div>
				<div class="col-md-4">
					<a href="http://www.aspdotnetstorefront.com/linkmanager.aspx?topic=10000skin&amp;type=expertskinning" class="thumbnail" target="_blank">
						<span class="thumbnail-items text-center">
							<img alt="Multiple people depicting a customer can hire the experts to help" src="(!SkinPath!)/images/box3.jpg" class="img-responsive center-block" />
						</span>
						<span class="thumbnail-items text-center">
							Hire the experts to help
						</span>
					</a>
				</div>
			</div>			
		</div>
		<div class="col-md-6">
			<div class="home-image">
				<img src="(!SkinPath!)/images/home4.jpg" alt="Welcome" class="img-responsive center-block" />
			</div>
		</div>
	</div>', 
	0
);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[Topic] WHERE [Name] = 'Template.Footer')
BEGIN
INSERT INTO Topic (Name, Title, [Description], ShowInSiteMap) VALUES 
(
	'Template.Footer', 
	'The footer section of the template', 
	'<div class="row">
		<div class="col-sm-12 col-md-3">
			<div class="list-wrapper">
				<h3 class="footer-heading">Location &amp; Hours</h3>
				<div class="footer-list">
					<div>1234 Main St.</div>
					<div>Ashland, OR 97520</div>
					<div>Phone: 541-867-5309</div>
					<div>M-F 9am - 5pm</div>
					<div>
						<a href="(!Url ActionName=''Index'' ControllerName=''ContactUs''!)">Contact Us</a>
					</div>
				</div>
			</div>
		</div>
		<div class="col-sm-12 col-md-3">
			<div class="list-wrapper">
				<h3 class="footer-heading">Store Policies</h3>
				<div class="footer-list">
					<div>
						<a href="(!TopicLink name=''security''!)">Security</a>
					</div>
					<div>
						<a href="(!TopicLink name=''privacy''!)">Privacy Policy</a>
					</div>
					<div>
						<a href="(!TopicLink name=''returns''!)">Return Policy</a>
					</div>
					<div>
						<a href="(!TopicLink name=''service''!)">Customer Service</a>
					</div>
				</div>
			</div>
		</div>
		<div class="col-sm-12 col-md-3">
			<div class="list-wrapper">
				<h3 class="footer-heading">Store Information</h3>
				<div class="footer-list">
					<div>
						<a href="(!Url ActionName=''Index'' ControllerName=''Account''!)">Account</a>
					</div>
					<div>
						<a href="(!Url ActionName=''Index'' ControllerName=''Account''!)#OrderHistory">Order Tracking</a>
					</div>
					<div>
						<a href="(!Url ActionName=''Index'' ControllerName=''SiteMap''!)">Site Map</a>
					</div>
				</div>
			</div>
		</div>
		<div class="col-sm-12 col-md-3">
			<div class="footer-list">
				<div>
					<div class="social-links">
						<a aria-label="facebook" target="_blank" href="http://www.aspdotnetstorefront.com/linkmanager.aspx?topic=10000skin&amp;type=facebook">
							<em class="icon fa fa-facebook">
								<span class="hidden">Facebook</span>
							</em>
						</a>
						<a aria-label="instagram" target="_blank" href="http://www.aspdotnetstorefront.com/linkmanager.aspx?topic=10000skin&amp;type=instagram">
							<em class="icon fa fa-instagram">
								<span class="hidden">Instagram</span>
							</em>
						</a>
						<a aria-label="pinterest" target="_blank" href="http://www.aspdotnetstorefront.com/linkmanager.aspx?topic=10000skin&amp;type=pinterest">
							<em class="icon fa fa-pinterest">
								<span class="hidden">Pinterest</span>
							</em>
						</a>
						<a aria-label="twitter" target="_blank" href="http://www.aspdotnetstorefront.com/linkmanager.aspx?topic=10000skin&amp;type=twitter">
							<em class="icon fa fa-twitter">
								<span class="hidden">Twitter</span>
							</em>
						</a>
						<a aria-label="youtube" target="_blank" href="http://www.aspdotnetstorefront.com/linkmanager.aspx?topic=10000skin&amp;type=youtube">
							<em class="icon fa fa-youtube">
								<span class="hidden">YouTube</span>
							</em>
						</a>
					</div>
				</div>
				<div>
					<div class="seal-marker">
						<img alt="Powered by AspDotNetStorefront" src="(!SkinPath!)/images/seal.png" />
					</div>
				</div>
			</div>
		</div>
	</div>', 
	0
);
END
GO

--reCAPTCHA
UPDATE AppConfig SET Name = 'reCAPTCHA.RequiredOnContactForm' WHERE Name = 'ContactUs.UseCaptcha'
UPDATE AppConfig SET Name = 'reCAPTCHA.RequiredOnCheckout' WHERE Name = 'SecurityCodeRequiredOnCheckout'
UPDATE AppConfig SET Name = 'reCAPTCHA.RequiredOnCreateAccount' WHERE Name = 'SecurityCodeRequiredOnCreateAccount'
UPDATE AppConfig SET Name = 'reCAPTCHA.RequiredOnStoreLogin' WHERE Name = 'SecurityCodeRequiredOnStoreLogin'

DELETE FROM AppConfig WHERE NAME = 'Captcha.AllowedCharactersRegex'
DELETE FROM AppConfig WHERE NAME = 'Captcha.CaseSensitive'
DELETE FROM AppConfig WHERE NAME = 'Captcha.MaxAsciiValue'
DELETE FROM AppConfig WHERE NAME = 'Captcha.NumberOfCharacters'

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'reCAPTCHA.SiteKey')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'reCAPTCHA.SiteKey', 
		'The ''Site Key'' value from your Google reCAPTCHA account.', 
		'string', 
		NULL, 
		'', 
		'SECURITY', 0, 0);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'reCAPTCHA.SecretKey')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'reCAPTCHA.SecretKey', 
		'The ''Secret Key'' value from your Google reCAPTCHA account.', 
		'string', 
		NULL, 
		'', 
		'SECURITY', 0, 0);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'reCAPTCHA.VerifyURL')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ValueType], [AllowableValues], [ConfigValue], [GroupName], [SuperOnly], [Hidden])
	VALUES 
	(
		'reCAPTCHA.VerifyURL', 
		'Do not change this unless instructed to by support.', 
		'string', 
		NULL, 
		'https://www.google.com/recaptcha/api/siteverify', 
		'SECURITY', 0, 0);
END
GO

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_GetFeaturedProducts]'))
	DROP PROC [dbo].[aspdnsf_GetFeaturedProducts]
GO

CREATE procedure [dbo].[aspdnsf_GetFeaturedProducts](
	@NumHomePageFeaturedProducts	int,
	@CustomerLevelID				int,
	@InventoryFilter				int = 0,
	@StoreID						int = 1,
	@FilterProduct					bit = 0 )
as begin
	set nocount on

	select top (@NumHomePageFeaturedProducts)
		p.ProductID,
		p.ImageFilenameOverride,
		p.SKU,
		p.SEName,
		p.Name,
		p.Description,
		p.TaxClassID,
		pv.VariantID,
		p.HidePriceUntilCart,
		pv.name VariantName,
		pv.Price,
		pv.Description VariantDescription,
		isnull(pv.SalePrice, 0) SalePrice,
		isnull(SkuSuffix, '') SkuSuffix,
		pv.Dimensions,
		pv.Weight,
		isnull(pv.Points, 0) Points,
		pv.Inventory,
		pv.ImageFilenameOverride VariantImageFilenameOverride,
		pv.isdefault,
		pv.CustomerEntersPrice,
		isnull(pv.colors, '') Colors,
		isnull(pv.sizes, '') Sizes,
		sp.name SalesPromptName,
		case
			when pcl.productid is null then 0
			else isnull(e.Price, 0)
		end ExtendedPrice,
		p.SEAltText
	from
		Product p with (nolock)
		inner join ProductVariant pv with (nolock) on pv.ProductID = p.ProductID
		join dbo.SalesPrompt sp with (nolock) on p.SalesPromptID = sp.SalesPromptID
		left join dbo.ExtendedPrice e  with (nolock) on pv.VariantID=e.VariantID and e.CustomerLevelID = @CustomerLevelID
		left join dbo.ProductCustomerLevel pcl with (nolock) on p.ProductID = pcl.ProductID and pcl.CustomerLevelID = @CustomerLevelID
		left join (select VariantID, sum(quan) quan from dbo.Inventory with (nolock) group by VariantID) i  on pv.VariantID = i.VariantID
		left join ProductStore ps with (nolock) on ps.ProductID = p.ProductID and ps.StoreID = @StoreID and @FilterProduct = 1
	where
		p.IsFeatured=1
		and p.Deleted=0
		and p.Published = 1
		and pv.IsDefault = 1
		and (
			(case p.TrackInventoryBySizeAndColor
				when 1 then isnull(i.quan, 0)
				else pv.inventory
			end >= @InventoryFilter)
			or @InventoryFilter = -1 )
		and (@FilterProduct = 0 or ps.ProductID is not null)
	order by
		newid()
end
GO

IF NOT EXISTS(SELECT * FROM [dbo].[Topic] WHERE [Name] = 'HomePage.HomeImage')
BEGIN
INSERT INTO Topic (Name, Title, [Description], ShowInSiteMap) VALUES 
(
	'HomePage.HomeImage', 
	'The main home page image area', 
	'<div class="home-image home-main-image">
		<h1>
			<img src="(!SkinPath!)/images/home.jpg" alt="Home Page" class="img-responsive center-block" />
		</h1>
	</div>', 
	0
);
END
GO

/*********** End 10.0.14 Changes *******************/

/*********** Begin 10.0.15 Changes *******************/

ALTER TABLE dbo.Customer
	DROP CONSTRAINT DF_Customer_OkToEmail
GO
ALTER TABLE dbo.Customer ADD CONSTRAINT
	DF_Customer_OkToEmail DEFAULT ((0)) FOR OkToEmail
GO

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_insCustomer]'))
	DROP PROC [dbo].[aspdnsf_insCustomer]
GO

CREATE proc [dbo].[aspdnsf_insCustomer]
	@Email nvarchar(100),
	@Password nvarchar(100),
	@SkinID int,
	@AffiliateID int,
	@Referrer nvarchar(max),
	@IsAdmin tinyint,
	@LastIPAddress varchar(40),
	@LocaleSetting nvarchar(10),
	@Over13Checked tinyint,
	@OkToEmail tinyint = 0,
	@CurrencySetting nvarchar(10),
	@VATSetting int,
	@VATRegistrationID nvarchar(100),
	@CustomerLevelID int,
	@StoreID int = 1,
	@CustomerID int output

as begin
	set nocount on

	insert dbo.Customer(CustomerGUID, CustomerLevelID, RegisterDate, Email, Password, Gender, FirstName, LastName, Notes, SkinID, Phone, AffiliateID, Referrer, CouponCode, OkToEmail, IsAdmin, BillingEqualsShipping, LastIPAddress, OrderNotes, RTShipRequest, RTShipResponse, OrderOptions, LocaleSetting, MicroPayBalance, RecurringShippingMethodID, RecurringShippingMethod, BillingAddressID, ShippingAddressID, ExtensionData, FinalizationData, Deleted, CreatedOn, Over13Checked, CurrencySetting, VATSetting, VATRegistrationID, StoreCCInDB, IsRegistered, LockedUntil, AdminCanViewCC, PwdChanged, BadLoginCount, LastBadLogin, Active, PwdChangeRequired, SaltKey, StoreID)
	values
	(
		newid(),
		@CustomerLevelID,
		getdate(),
		isnull(@Email, ''),
		isnull(@Password, ''),
		null,
		null,
		null,
		null,
		isnull(@SkinID, 1),
		null,
		@AffiliateID,
		@Referrer,
		null,
		isnull(@OkToEmail, 0),
		isnull(@IsAdmin, 0),
		0,
		@LastIPAddress,
		null,
		null,
		null,
		null,
		isnull(@LocaleSetting, ('en-US')),
		0.0,
		1,
		null,
		null,
		null,
		null,
		null,
		0,
		getdate(),
		@Over13Checked,
		@CurrencySetting,
		@VATSetting,
		@VATRegistrationID,
		1,
		0,
		null,
		1,
		getdate(),
		0,
		null,
		1,
		0,
		0,
		@StoreID
	)
	
	set @CustomerID = scope_identity()
end
GO


IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[CustomerDataRetention]'))
	BEGIN
		CREATE TABLE [dbo].[CustomerDataRetention](
		[CustomerId] [int] NOT NULL,
		[RemovalRequestDate] [datetime] NULL,
		[RemovalDate] [datetime] NULL,
		[CustomerLastActiveOn] [datetime] NULL,
		 CONSTRAINT [PK_CustomerDataRetention] PRIMARY KEY CLUSTERED 
		(
			[CustomerId] ASC
		)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
		) ON [PRIMARY]

	END
GO	

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'DataRetentionPolicies.Enabled')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden]) 
	VALUES 
	(
		'DataRetentionPolicies.Enabled',
		'Enable data retention policies, including manual anonymization, and, when enabled with "DataRetentionPolicies.MonthsBeforeUserDataAnonymized", automatic anonymization of customer personally identifiable information.', 
		'false', 
		'boolean', 
		NULL, 
		'SECURITY', 0, 0);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'DataRetentionPolicies.MonthsBeforeUserDataAnonymized')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden]) 
	VALUES 
	(
		'DataRetentionPolicies.MonthsBeforeUserDataAnonymized',
		'When "DataRetentionPolicies.Enabled" is true, the number of months before customer personally identifiable information is automatically anonymized by database maintenance.', 
		'0', 
		'integer', 
		NULL, 
		'SECURITY', 0, 0);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[Topic] WHERE [Name] = 'removeaccountconfirmation')
BEGIN
INSERT INTO Topic (Name, Title, [Description], ShowInSiteMap) VALUES
(
	'removeaccountconfirmation', 
	'Remove Account Confirmation', 
	'Thank you for letting us know that you would like your personally identifiable information to be removed from our records. As long as no other law prevails, we will do so at once.',
	0
);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[Topic] WHERE [Name] = 'removeaccount')
BEGIN
INSERT INTO Topic (Name, Title, [Description], ShowInSiteMap) VALUES
(
	'removeaccount', 
	'Remove Account', 
	'If you wish your personally identifiable information (name; street address; email address; phone number) to be removed from our records, please let us know. Note, however, that other legal frameworks might prevent us from removing your data as quickly as you would choose. Your data is protected at all times and we will only ever keep it as long as the law requires. Beyond that time, we will remove it immediately upon your request.',
	0
);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[Topic] WHERE [Name] = 'removeaccountemail')
BEGIN
INSERT INTO Topic (Name, Title, [Description], ShowInSiteMap) VALUES
(
	'removeaccountemail', 
	'Remove Account Email', 
	'Thank you for letting us know that you would like your personally identifiable information to be removed from our records. We have really appreciated your business and we are proud to say that we have always kept your data safe. As long as no other law prevails, we will now remove it at once.',
	0
);
END
GO

IF NOT EXISTS(SELECT * FROM [dbo].[Topic] WHERE [Name] = 'dataretentionpolicy')
BEGIN
	INSERT INTO [dbo].[Topic] ([Name], [Title], [Description], [SETitle], [SEDescription], [SEKeywords], [Password], [RequiresDisclaimer], [XmlPackage], [ExtensionData], [ShowInSiteMap], [SkinID], [HTMLOk], [Deleted], [StoreID], [DisplayOrder], [CreatedOn], [Published], [UpdatedOn], [IsFrequent]) VALUES (N'dataretentionpolicy', N'Data Retention Policy', N'Note to <a href="http://www.aspdotnetstorefront.com/">AspDotNetStorefront</a> Administrators:<br/><br/>You can edit this placeholder text by editing the "Data Retention Policy" topic within the Admin Console.

	<h2>Your personal data is important to us</h2>
	<h3>What we need to collect</h3>
	<div>
	We only collect basic personal data about you which does not include any special types of information or location based information. This does however include name, address, email etc. We collect only the data that is necessary for us to contract with you, when you purchase goods or services from us. Our company will be what''s known as the ''Controller'' of the personal data you provide to us. 
	</div>
	<h3>Why we need it</h3>
	<div>We need to collect data that is required to process an online payment or deliver your purchased product. In such cases, we don''t need to get your consent (since we can''t fulfill our contract without the data) but we still want you to know how carefully we manage the protection of that data. We will not collect any personal data from you that we do not need in order to provide and oversee this service to you.</div>
	<h3>What we do with it</h3>
	<div>All the personal data we process is processed by our staff, and, for the purposes of IT hosting and maintenance this information is located on servers beyond our own building. No third-parties have access to your personal data unless the law allows them to do so. We have a Data Protection regime in place to oversee the effective and secure processing of your personal data. Our online store allows our administrators to anonymize and delete your personal data in a timely and responsible manner and also allows you to request the immediate deletion of your data once the transaction is full and final. You will find an option to delete the data on your account page.
	</div>
	<h3>How long we keep it</h3>
	<div>We have a formal retention period which requires us to keep your basic personal data (name, address, contact details) for 6 months, beyond which time it will be ''anonymized''. The term ''anonymized'' means that we will maintain a record of your transactions, but that it cannot be tracked to you as an individual.
	</div>
	<h3>What we would also like to do with it</h3>
	<div>We would however like to use your name and email address to inform you of our future offers and similar products. This information is not shared with third purposes and you can unsubscribe at any time via phone, email or our website.
	</div>
	<h3>What are your rights?</h3>
	<div>
	If at any point you believe the information we process on you is incorrect you can request to see this information and even have it corrected or deleted. If you wish to raise a complaint on how we have handled your personal data, you can contact our Data Protection Officer who will investigate the matter.
	If you are not satisfied with our response or believe we are processing your personal data not in accordance with the law you can complain to the Information Commissioner''s Office (ICO).
	</div>
	<div style="padding-top:15px;">
	Our Data Protection Officer can be contacted using our <a href="/contactus">online contact form</a>.
	</div>', NULL, NULL, NULL, NULL, 0, NULL, NULL, 1, 0, 1, 0, 0, 1, GETDATE(), 1, GETDATE(), 1)
END

IF NOT EXISTS(SELECT * FROM [dbo].[AppConfig] WHERE [Name] = 'Customer.OkToEmail.Default')
BEGIN
INSERT INTO [dbo].[AppConfig] ([Name], [Description], [ConfigValue], [ValueType], [AllowableValues], [GroupName], [SuperOnly], [Hidden]) 
	VALUES 
	(
		'Customer.OkToEmail.Default', 
		'If true new customers default to OkayToEmail true.', 
		'false', 
		'boolean', 
		NULL, 
		'EMAIL', 0, 0);
END
GO

IF EXISTS (SELECT * FROM sys.triggers WHERE object_id = object_id(N'Customer_SyncCustomerDataRetention'))
	DROP TRIGGER [dbo].[Customer_SyncCustomerDataRetention]
GO

CREATE TRIGGER [dbo].[Customer_SyncCustomerDataRetention]
   ON [Customer]
   AFTER INSERT,DELETE
AS 
BEGIN
	IF @@rowcount = 0 RETURN;

	IF trigger_nestlevel(object_ID('Customer_SyncCustomerDataRetention')) > 1 RETURN;

	SET NOCOUNT ON;
	
	if exists(select * from Inserted)
	begin
		-- insert
		insert CustomerDataRetention
		(CustomerId, CustomerLastActiveOn)
		select CustomerId, getdate() from Inserted
	end

	if exists(select * from Deleted)
	begin
		-- delete
		delete from CustomerDataRetention
		where CustomerId in 
			(select CustomerId from Deleted)
	end
END
GO

if exists(select * from sys.tables where Name = 'DocumentType' and type = 'U')
begin
	DROP TABLE [dbo].[DocumentType]
	
	ALTER TABLE dbo.[Document]	
		DROP CONSTRAINT DF_Document_DocumentTypeID	
	
	DROP INDEX IX_Document_DocumentTypeID ON dbo.[Document]	
	
	ALTER TABLE dbo.[Document]	
		DROP COLUMN DocumentTypeID	
end
GO

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_DropAllNonPrimaryIndexes]'))
	DROP PROC [dbo].[aspdnsf_DropAllNonPrimaryIndexes]
GO

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_CreateIndexes]'))
	DROP PROC [dbo].[aspdnsf_CreateIndexes]
GO

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_DropAndRecreateAllNonPrimaryIndexes]'))
	DROP PROC [dbo].[aspdnsf_DropAndRecreateAllNonPrimaryIndexes]
GO

ALTER PROC [dbo].[aspdnsf_UpdateIndexes]
AS
BEGIN

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Address]') AND name = N'UIX_Address_AddressGUID') CREATE UNIQUE INDEX [UIX_Address_AddressGUID] ON [Address](AddressGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Address]') AND name = N'IX_Address_CustomerID') CREATE INDEX [IX_Address_CustomerID] ON [Address](CustomerID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Address]') AND name = N'IX_Address_Deleted') CREATE INDEX [IX_Address_Deleted] ON [Address](Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AffiliateActivity]') AND name = N'IX_AffiliateActivity_AffiliateID') CREATE INDEX [IX_AffiliateActivity_AffiliateID] ON [AffiliateActivity](AffiliateID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AffiliateActivity]') AND name = N'IX_AffiliateActivity_AffiliateID_ActivityDate') CREATE INDEX [IX_AffiliateActivity_AffiliateID_ActivityDate] ON [AffiliateActivity](AffiliateID, ActivityDate);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AffiliateCommissions]') AND name = N'IX_AffiliateCommissions_RowGUID') CREATE INDEX [IX_AffiliateCommissions_RowGUID] ON [AffiliateCommissions](RowGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AffiliateCommissions]') AND name = N'IX_AffiliateCommissions_LowValue') CREATE INDEX [IX_AffiliateCommissions_LowValue] ON [AffiliateCommissions](LowValue);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AffiliateCommissions]') AND name = N'IX_AffiliateCommissions_HighValue') CREATE INDEX [IX_AffiliateCommissions_HighValue] ON [AffiliateCommissions](HighValue);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AppConfig]') AND name = N'UIX_AppConfig_Name_StoreID') CREATE UNIQUE INDEX [UIX_AppConfig_Name_StoreID] ON [AppConfig](Name, StoreID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AppConfig]') AND name = N'IX_AppConfig_GroupName') CREATE INDEX [IX_AppConfig_GroupName] ON [AppConfig](GroupName);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[BadWord]') AND name = N'IX_BadWord') CREATE INDEX [IX_BadWord] ON [BadWord](Word);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Category]') AND name = N'IX_Category_Name') CREATE INDEX [IX_Category_Name] ON [Category](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Category]') AND name = N'IX_Category_Deleted') CREATE INDEX [IX_Category_Deleted] ON [Category](Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Category]') AND name = N'IX_Category_Published') CREATE INDEX [IX_Category_Published] ON [Category](Published);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Category]') AND name = N'IX_Category_Wholesale') CREATE INDEX [IX_Category_Wholesale] ON [Category](Wholesale);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Category]') AND name = N'IX_Category_CategoryGUID') CREATE INDEX [IX_Category_CategoryGUID] ON [Category](CategoryGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Category]') AND name = N'IX_Category_ParentCategoryID') CREATE INDEX [IX_Category_ParentCategoryID] ON [Category](ParentCategoryID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Category]') AND name = N'IX_Category_DisplayOrder') CREATE INDEX [IX_Category_DisplayOrder] ON [Category](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Category]') AND name = N'IX_Category_Deleted_Published') CREATE INDEX [IX_Category_Deleted_Published] ON [Category](Deleted, Published);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Category]') AND name = N'IX_Category_Deleted_Wholesale') CREATE INDEX [IX_Category_Deleted_Wholesale] ON [Category](Deleted, Wholesale);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Country]') AND name = N'UIX_Country_Name') CREATE UNIQUE INDEX [UIX_Country_Name] ON [Country](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Country]') AND name = N'IX_Country_DisplayOrder_Name') CREATE INDEX [IX_Country_DisplayOrder_Name] ON [Country](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Country]') AND name = N'IX_Country_CountryGUID') CREATE INDEX [IX_Country_CountryGUID] ON [Country](CountryGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Country]') AND name = N'IX_Country_DisplayOrder') CREATE INDEX [IX_Country_DisplayOrder] ON [Country](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CountryTaxRate]') AND name = N'UIX_CountryTaxRate_CountryID_TaxClassID') CREATE UNIQUE INDEX [UIX_CountryTaxRate_CountryID_TaxClassID] ON [CountryTaxRate](CountryID, TaxClassID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Coupon]') AND name = N'UIX_Coupon_CouponGUID') CREATE UNIQUE INDEX [UIX_Coupon_CouponGUID] ON [Coupon](CouponGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Coupon]') AND name = N'UIX_Coupon_CouponCode') CREATE UNIQUE INDEX [UIX_Coupon_CouponCode] ON [Coupon](CouponCode);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Coupon]') AND name = N'IX_Coupon_ExpirationDate') CREATE INDEX [IX_Coupon_ExpirationDate] ON [Coupon](ExpirationDate);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Coupon]') AND name = N'IX_Coupon_Deleted') CREATE INDEX [IX_Coupon_Deleted] ON [Coupon](Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CreditCardType]') AND name = N'UIX_CreditCardTypes') CREATE UNIQUE INDEX [UIX_CreditCardTypes] ON [CreditCardType](CardTypeGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CreditCardType]') AND name = N'IX_CreditCardType_CardType') CREATE INDEX [IX_CreditCardType_CardType] ON [CreditCardType](CardType);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Currency]') AND name = N'UIX_Currency_CurrencyGUID') CREATE UNIQUE INDEX [UIX_Currency_CurrencyGUID] ON [Currency](CurrencyGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Customer]') AND name = N'UIX_Customer_CustomerGUID') CREATE UNIQUE INDEX [UIX_Customer_CustomerGUID] ON [Customer](CustomerGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Customer]') AND name = N'IX_Customer_EMail') CREATE INDEX [IX_Customer_EMail] ON [Customer](Email);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Customer]') AND name = N'IX_Customer_Password') CREATE INDEX [IX_Customer_Password] ON [Customer](Password);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Customer]') AND name = N'IX_Customer_CustomerLevelID') CREATE INDEX [IX_Customer_CustomerLevelID] ON [Customer](CustomerLevelID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Customer]') AND name = N'IX_Customer_IsAdmin') CREATE INDEX [IX_Customer_IsAdmin] ON [Customer](IsAdmin);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Customer]') AND name = N'IX_Customer_OkToEMail') CREATE INDEX [IX_Customer_OkToEMail] ON [Customer](OkToEmail);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Customer]') AND name = N'IX_Customer_Deleted') CREATE INDEX [IX_Customer_Deleted] ON [Customer](Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Customer]') AND name = N'IX_Customer_AffiliateID') CREATE INDEX [IX_Customer_AffiliateID] ON [Customer](AffiliateID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Customer]') AND name = N'IX_Customer_CouponCode') CREATE INDEX [IX_Customer_CouponCode] ON [Customer](CouponCode);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Customer]') AND name = N'IX_Customer_CreatedOn') CREATE INDEX [IX_Customer_CreatedOn] ON [Customer](CreatedOn);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CustomerLevel]') AND name = N'IX_CustomerLevel_Deleted') CREATE INDEX [IX_CustomerLevel_Deleted] ON [CustomerLevel](Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CustomerLevel]') AND name = N'IX_CustomerLevel_Name') CREATE INDEX [IX_CustomerLevel_Name] ON [CustomerLevel](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CustomerLevel]') AND name = N'IX_CustomerLevel_DisplayOrder') CREATE INDEX [IX_CustomerLevel_DisplayOrder] ON [CustomerLevel](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CustomerLevel]') AND name = N'IX_CustomerLevel_DisplayOrder_Name') CREATE INDEX [IX_CustomerLevel_DisplayOrder_Name] ON [CustomerLevel](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CustomerSession]') AND name = N'IX_CustomerSession_CustomerID') CREATE INDEX [IX_CustomerSession_CustomerID] ON [CustomerSession](CustomerID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Distributor]') AND name = N'UIX_Distributor_DistributorGUID') CREATE UNIQUE INDEX [UIX_Distributor_DistributorGUID] ON [Distributor](DistributorGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Distributor]') AND name = N'IX_Distributor_DisplayOrder') CREATE INDEX [IX_Distributor_DisplayOrder] ON [Distributor](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Distributor]') AND name = N'IX_Distributor_Name') CREATE INDEX [IX_Distributor_Name] ON [Distributor](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Distributor]') AND name = N'IX_Distributor_DisplayOrder_Name') CREATE INDEX [IX_Distributor_DisplayOrder_Name] ON [Distributor](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Document]') AND name = N'UIX_Document_DocumentGUID') CREATE UNIQUE INDEX [UIX_Document_DocumentGUID] ON [Document](DocumentGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Document]') AND name = N'IX_Document_Published') CREATE INDEX [IX_Document_Published] ON [Document](Published);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Document]') AND name = N'IX_Document_Wholesale') CREATE INDEX [IX_Document_Wholesale] ON [Document](Wholesale);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Document]') AND name = N'IX_Document_Deleted') CREATE INDEX [IX_Document_Deleted] ON [Document](Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Document]') AND name = N'IX_Document_Published_Deleted') CREATE INDEX [IX_Document_Published_Deleted] ON [Document](Published, Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Document]') AND name = N'IX_Document_Wholesale_Deleted') CREATE INDEX [IX_Document_Wholesale_Deleted] ON [Document](Wholesale, Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Document]') AND name = N'IX_Document_Name') CREATE INDEX [IX_Document_Name] ON [Document](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[EventHandler]') AND name = N'UIX_EventhHandler') CREATE UNIQUE INDEX [UIX_EventhHandler] ON [EventHandler](EventName);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ExtendedPrice]') AND name = N'UIX_ExtendedPrice_2') CREATE UNIQUE INDEX [UIX_ExtendedPrice_2] ON [ExtendedPrice](ExtendedPriceGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ExtendedPrice]') AND name = N'IX_ExtendedPrice_VariantID_CustomerLevelID') CREATE INDEX [IX_ExtendedPrice_VariantID_CustomerLevelID] ON [ExtendedPrice](VariantID, CustomerLevelID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ExtendedPrice]') AND name = N'IX_ExtendedPrice_VariantID') CREATE INDEX [IX_ExtendedPrice_VariantID] ON [ExtendedPrice](VariantID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[FailedTransaction]') AND name = N'IX_FailedTransaction_OrderDate') CREATE INDEX [IX_FailedTransaction_OrderDate] ON [FailedTransaction](OrderDate);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[FailedTransaction]') AND name = N'IX_FailedTransaction_PaymentGateway') CREATE INDEX [IX_FailedTransaction_PaymentGateway] ON [FailedTransaction](PaymentGateway);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Feed]') AND name = N'UIX_Feed_FeedGUID') CREATE UNIQUE INDEX [UIX_Feed_FeedGUID] ON [Feed](FeedGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Feed]') AND name = N'IX_Feed_DisplayOrder') CREATE INDEX [IX_Feed_DisplayOrder] ON [Feed](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Feed]') AND name = N'IX_Feed_DisplayOrder_Name') CREATE INDEX [IX_Feed_DisplayOrder_Name] ON [Feed](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Genre]') AND name = N'UIX_Genre_GenreGUID') CREATE UNIQUE INDEX [UIX_Genre_GenreGUID] ON [Genre](GenreGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Genre]') AND name = N'IX_Genre_Name') CREATE INDEX [IX_Genre_Name] ON [Genre](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Genre]') AND name = N'IX_Genre_DisplayOrder_Name') CREATE INDEX [IX_Genre_DisplayOrder_Name] ON [Genre](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[GiftCard]') AND name = N'UIX_GiftCard_GiftCardGUID') CREATE UNIQUE INDEX [UIX_GiftCard_GiftCardGUID] ON [GiftCard](GiftCardGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[GiftCard]') AND name = N'IX_GiftCard_SerialNumber') CREATE INDEX [IX_GiftCard_SerialNumber] ON [GiftCard](SerialNumber);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[GiftCard]') AND name = N'IX_GiftCard_ExpirationDate') CREATE INDEX [IX_GiftCard_ExpirationDate] ON [GiftCard](ExpirationDate);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[GiftCard]') AND name = N'IX_GiftCard_CreatedOn') CREATE INDEX [IX_GiftCard_CreatedOn] ON [GiftCard](CreatedOn);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[GiftCard]') AND name = N'IX_GiftCard_PurchasedByCustomerID') CREATE INDEX [IX_GiftCard_PurchasedByCustomerID] ON [GiftCard](PurchasedByCustomerID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[GiftCardUsage]') AND name = N'UIX_GiftCardUsage_GiftCardUsageGUID') CREATE UNIQUE INDEX [UIX_GiftCardUsage_GiftCardUsageGUID] ON [GiftCardUsage](GiftCardUsageGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[GiftCardUsage]') AND name = N'IX_GiftCardUsage_GiftCardID') CREATE INDEX [IX_GiftCardUsage_GiftCardID] ON [GiftCardUsage](GiftCardID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[GiftCardUsage]') AND name = N'IX_GiftCardUsage_UsedByCustomerID') CREATE INDEX [IX_GiftCardUsage_UsedByCustomerID] ON [GiftCardUsage](UsedByCustomerID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Inventory]') AND name = N'UIX_Inventory_InventoryGUID') CREATE UNIQUE INDEX [UIX_Inventory_InventoryGUID] ON [Inventory](InventoryGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Inventory]') AND name = N'IX_Inventory_VariantID_Color_Size') CREATE INDEX [IX_Inventory_VariantID_Color_Size] ON [Inventory](VariantID, Color, Size);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitCart]') AND name = N'IX_KitCart_CreatedOn') CREATE INDEX [IX_KitCart_CreatedOn] ON [KitCart](CreatedOn);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitCart]') AND name = N'IX_KitCart_ShoppingCartRecID') CREATE INDEX [IX_KitCart_ShoppingCartRecID] ON [KitCart](ShoppingCartRecID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitCart]') AND name = N'IX_KitCart_CustomerID_ShoppingCartRecID') CREATE INDEX [IX_KitCart_CustomerID_ShoppingCartRecID] ON [KitCart](CustomerID, ShoppingCartRecID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitCart]') AND name = N'IX_KitCart_ProductID') CREATE INDEX [IX_KitCart_ProductID] ON [KitCart](ProductID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitCart]') AND name = N'IX_KitCart_VariantID') CREATE INDEX [IX_KitCart_VariantID] ON [KitCart](VariantID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitCart]') AND name = N'IX_KitCart_KitGroupID') CREATE INDEX [IX_KitCart_KitGroupID] ON [KitCart](KitGroupID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitCart]') AND name = N'IX_KitCart_KitItemID') CREATE INDEX [IX_KitCart_KitItemID] ON [KitCart](KitItemID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitGroup]') AND name = N'UIX_KitGroup_KitGroupGUID') CREATE UNIQUE INDEX [UIX_KitGroup_KitGroupGUID] ON [KitGroup](KitGroupGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitGroup]') AND name = N'IX_KitGroup_ProductID') CREATE INDEX [IX_KitGroup_ProductID] ON [KitGroup](ProductID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitGroup]') AND name = N'IX_KitGroup_ProductID_DisplayOrder') CREATE INDEX [IX_KitGroup_ProductID_DisplayOrder] ON [KitGroup](ProductID, DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitGroup]') AND name = N'IX_KitGroup_DisplayOrder') CREATE INDEX [IX_KitGroup_DisplayOrder] ON [KitGroup](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitGroupType]') AND name = N'UIX_KitGroupType_KitGroupTypeGUID') CREATE UNIQUE INDEX [UIX_KitGroupType_KitGroupTypeGUID] ON [KitGroupType](KitGroupTypeGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitGroupType]') AND name = N'IX_KitGroupType_DisplayOrder') CREATE INDEX [IX_KitGroupType_DisplayOrder] ON [KitGroupType](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitGroupType]') AND name = N'IX_KitGroupType_DisplayOrder_Name') CREATE INDEX [IX_KitGroupType_DisplayOrder_Name] ON [KitGroupType](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitGroupType]') AND name = N'IX_KitGroupType_Name') CREATE INDEX [IX_KitGroupType_Name] ON [KitGroupType](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitItem]') AND name = N'UIX_KitItem_KitItemGUID') CREATE UNIQUE INDEX [UIX_KitItem_KitItemGUID] ON [KitItem](KitItemGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitItem]') AND name = N'IX_KitItem_KitGroupID') CREATE INDEX [IX_KitItem_KitGroupID] ON [KitItem](KitGroupID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitItem]') AND name = N'IX_KitItem_KitGroupID_DisplayOrder') CREATE INDEX [IX_KitItem_KitGroupID_DisplayOrder] ON [KitItem](KitGroupID, DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitItem]') AND name = N'IX_KitItem_DisplayOrder') CREATE INDEX [IX_KitItem_DisplayOrder] ON [KitItem](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitItem]') AND name = N'IX_KitItem_DisplayOrder_Name') CREATE INDEX [IX_KitItem_DisplayOrder_Name] ON [KitItem](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[KitItem]') AND name = N'IX_KitItem_Name') CREATE INDEX [IX_KitItem_Name] ON [KitItem](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Library]') AND name = N'IX_Library_Deleted') CREATE INDEX [IX_Library_Deleted] ON [Library](Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Library]') AND name = N'IX_Library_Published') CREATE INDEX [IX_Library_Published] ON [Library](Published);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Library]') AND name = N'IX_Library_Wholesale') CREATE INDEX [IX_Library_Wholesale] ON [Library](Wholesale);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Library]') AND name = N'IX_Library_LibraryGUID') CREATE INDEX [IX_Library_LibraryGUID] ON [Library](LibraryGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Library]') AND name = N'IX_Library_ParentLibraryID') CREATE INDEX [IX_Library_ParentLibraryID] ON [Library](ParentLibraryID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Library]') AND name = N'IX_Library_DisplayOrder') CREATE INDEX [IX_Library_DisplayOrder] ON [Library](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Library]') AND name = N'IX_Library_Deleted_Published') CREATE INDEX [IX_Library_Deleted_Published] ON [Library](Deleted, Published);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Library]') AND name = N'IX_Library_Deleted_Wholesale') CREATE INDEX [IX_Library_Deleted_Wholesale] ON [Library](Deleted, Wholesale);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Library]') AND name = N'IX_Library_Name') CREATE INDEX [IX_Library_Name] ON [Library](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Library]') AND name = N'IX_Library_DisplayOrder_Name') CREATE INDEX [IX_Library_DisplayOrder_Name] ON [Library](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[LocaleSetting]') AND name = N'UIX_Locale_LocaleSettingGUID') CREATE UNIQUE INDEX [UIX_Locale_LocaleSettingGUID] ON [LocaleSetting](LocaleSettingGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[LocaleSetting]') AND name = N'UIX_Locale_Name') CREATE UNIQUE INDEX [UIX_Locale_Name] ON [LocaleSetting](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[LocaleSetting]') AND name = N'IX_Locale_DisplayOrder_Name') CREATE INDEX [IX_Locale_DisplayOrder_Name] ON [LocaleSetting](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[LocaleSetting]') AND name = N'IX_Locale_DisplayOrder') CREATE INDEX [IX_Locale_DisplayOrder] ON [LocaleSetting](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Manufacturer]') AND name = N'UIX_Manufacturer_ManufacturerGUID') CREATE UNIQUE INDEX [UIX_Manufacturer_ManufacturerGUID] ON [Manufacturer](ManufacturerGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Manufacturer]') AND name = N'IX_Manufacturer_Deleted') CREATE INDEX [IX_Manufacturer_Deleted] ON [Manufacturer](Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Manufacturer]') AND name = N'IX_Manufacturer_DisplayOrder') CREATE INDEX [IX_Manufacturer_DisplayOrder] ON [Manufacturer](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Manufacturer]') AND name = N'IX_Manufacturer_Name') CREATE INDEX [IX_Manufacturer_Name] ON [Manufacturer](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Manufacturer]') AND name = N'IX_Manufacturer_DisplayOrder_Name') CREATE INDEX [IX_Manufacturer_DisplayOrder_Name] ON [Manufacturer](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[News]') AND name = N'UIX_News_NewsGUID') CREATE UNIQUE INDEX [UIX_News_NewsGUID] ON [News](NewsGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[News]') AND name = N'IX_News_ExpiresOn') CREATE INDEX [IX_News_ExpiresOn] ON [News](ExpiresOn DESC);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[News]') AND name = N'IX_News_Deleted') CREATE INDEX [IX_News_Deleted] ON [News](Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[News]') AND name = N'IX_News_Published') CREATE INDEX [IX_News_Published] ON [News](Published);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[News]') AND name = N'IX_News_Wholesale') CREATE INDEX [IX_News_Wholesale] ON [News](Wholesale);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[OrderNumbers]') AND name = N'UIX_OrderNumbers_OrderNumberGUID') CREATE UNIQUE INDEX [UIX_OrderNumbers_OrderNumberGUID] ON [OrderNumbers](OrderNumberGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[OrderNumbers]') AND name = N'IX_OrderNumbers_CreatedOn') CREATE INDEX [IX_OrderNumbers_CreatedOn] ON [OrderNumbers](CreatedOn);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'IX_Orders_OrderNumber') CREATE INDEX [IX_Orders_OrderNumber] ON [Orders](OrderNumber);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'IX_Orders_ParentOrderNumber') CREATE INDEX [IX_Orders_ParentOrderNumber] ON [Orders](ParentOrderNumber);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'IX_Orders_CustomerID') CREATE INDEX [IX_Orders_CustomerID] ON [Orders](CustomerID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'IX_Orders_OrderNumber_CustomerID') CREATE INDEX [IX_Orders_OrderNumber_CustomerID] ON [Orders](OrderNumber, CustomerID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'IX_Orders_AffiliateID') CREATE INDEX [IX_Orders_AffiliateID] ON [Orders](AffiliateID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'IX_Orders_OrderDate') CREATE INDEX [IX_Orders_OrderDate] ON [Orders](OrderDate);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'IX_Orders_OrderGUID') CREATE INDEX [IX_Orders_OrderGUID] ON [Orders](OrderGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'IX_Orders_EMail') CREATE INDEX [IX_Orders_EMail] ON [Orders](Email);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'IX_Orders_IsNew') CREATE INDEX [IX_Orders_IsNew] ON [Orders](IsNew);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'IX_Orders_CouponCode') CREATE INDEX [IX_Orders_CouponCode] ON [Orders](CouponCode);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'IX_Orders_TransactionState') CREATE INDEX [IX_Orders_TransactionState] ON [Orders](TransactionState);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders_KitCart]') AND name = N'IX_Orders_KitCart_OrderNumber') CREATE CLUSTERED INDEX [IX_Orders_KitCart_OrderNumber] ON [Orders_KitCart](OrderNumber);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders_KitCart]') AND name = N'IX_Orders_KitCart_ProductID_VariantID') CREATE INDEX [IX_Orders_KitCart_ProductID_VariantID] ON [Orders_KitCart](ProductID, VariantID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders_KitCart]') AND name = N'IX_Orders_KitCart_CreatedOn') CREATE INDEX [IX_Orders_KitCart_CreatedOn] ON [Orders_KitCart](CreatedOn);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders_KitCart]') AND name = N'IX_Orders_KitCart_KitCartRecID') CREATE INDEX [IX_Orders_KitCart_KitCartRecID] ON [Orders_KitCart](KitCartRecID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders_KitCart]') AND name = N'IX_Orders_KitCart_CustomerID') CREATE INDEX [IX_Orders_KitCart_CustomerID] ON [Orders_KitCart](CustomerID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders_KitCart]') AND name = N'IX_Orders_KitCart_ShoppingCartRecID') CREATE INDEX [IX_Orders_KitCart_ShoppingCartRecID] ON [Orders_KitCart](ShoppingCartRecID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders_KitCart]') AND name = N'IX_Orders_KitCart_KitGroupID') CREATE INDEX [IX_Orders_KitCart_KitGroupID] ON [Orders_KitCart](KitGroupID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders_ShoppingCart]') AND name = N'IX_Orders_ShoppingCart_OrderNumber_CustomerID') CREATE CLUSTERED INDEX [IX_Orders_ShoppingCart_OrderNumber_CustomerID] ON [Orders_ShoppingCart](OrderNumber, CustomerID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders_ShoppingCart]') AND name = N'IX_Orders_ShoppingCart_OrderedProductSKU') CREATE INDEX [IX_Orders_ShoppingCart_OrderedProductSKU] ON [Orders_ShoppingCart](OrderedProductSKU);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders_ShoppingCart]') AND name = N'IX_Orders_ShoppingCart_CustomerID') CREATE INDEX [IX_Orders_ShoppingCart_CustomerID] ON [Orders_ShoppingCart](CustomerID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders_ShoppingCart]') AND name = N'IX_Orders_ShoppingCart_ShoppingCartRecID') CREATE INDEX [IX_Orders_ShoppingCart_ShoppingCartRecID] ON [Orders_ShoppingCart](ShoppingCartRecID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders_ShoppingCart]') AND name = N'IX_Orders_ShoppingCart_ProductID') CREATE INDEX [IX_Orders_ShoppingCart_ProductID] ON [Orders_ShoppingCart](ProductID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders_ShoppingCart]') AND name = N'IX_Orders_ShoppingCart_ProductID_VariantID_ChosenColor_ChosenSize') CREATE INDEX [IX_Orders_ShoppingCart_ProductID_VariantID_ChosenColor_ChosenSize] ON [Orders_ShoppingCart](ProductID, VariantID, ChosenColor, ChosenSize);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders_ShoppingCart]') AND name = N'IX_Orders_ShoppingCart_CreatedOn') CREATE INDEX [IX_Orders_ShoppingCart_CreatedOn] ON [Orders_ShoppingCart](CreatedOn);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PasswordLog]') AND name = N'CIX_PasswordLog') CREATE CLUSTERED INDEX [CIX_PasswordLog] ON [PasswordLog](CustomerID, ChangeDt);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = N'UIX_Product_ProductGUID') CREATE UNIQUE INDEX [UIX_Product_ProductGUID] ON [Product](ProductGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = N'IX_Product_SKU') CREATE INDEX [IX_Product_SKU] ON [Product](SKU);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = N'IX_Product_IsImport') CREATE INDEX [IX_Product_IsImport] ON [Product](IsImport);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = N'IX_Product_IsSystem') CREATE INDEX [IX_Product_IsSystem] ON [Product](IsSystem);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = N'IX_Product_Published') CREATE INDEX [IX_Product_Published] ON [Product](Published);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = N'IX_Product_Wholesale') CREATE INDEX [IX_Product_Wholesale] ON [Product](Wholesale);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = N'IX_Product_Deleted') CREATE INDEX [IX_Product_Deleted] ON [Product](Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = N'IX_Product_ProductTypeID') CREATE INDEX [IX_Product_ProductTypeID] ON [Product](ProductTypeID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = N'IX_Product_IsAKit') CREATE INDEX [IX_Product_IsAKit] ON [Product](IsAKit);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = N'IX_Product_Name') CREATE INDEX [IX_Product_Name] ON [Product](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = N'IX_Product_ManufacturerPartNumber') CREATE INDEX [IX_Product_ManufacturerPartNumber] ON [Product](ManufacturerPartNumber);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = N'IX_Product_Published_Deleted') CREATE INDEX [IX_Product_Published_Deleted] ON [Product](Published, Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = N'IX_Product_Wholesale_Deleted') CREATE INDEX [IX_Product_Wholesale_Deleted] ON [Product](Wholesale, Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = N'IX_Product_Published_Deleted_IsAKit') CREATE NONCLUSTERED INDEX [IX_Product_Published_Deleted_IsAKit] ON [dbo].[Product] ([Published],[Deleted],[IsAKit]) INCLUDE ([ProductID],[Name],[ProductTypeID],[TrackInventoryBySizeAndColor])
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductCategory]') AND name = N'IX_Product_ProductID') CREATE INDEX [IX_Product_ProductID] ON [ProductCategory](ProductID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductCategory]') AND name = N'IX_Category_CategoryID') CREATE INDEX [IX_Category_CategoryID] ON [ProductCategory](CategoryID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductSection]') AND name = N'IX_ProductSection_SectionID_DisplayOrder') CREATE INDEX [IX_ProductSection_SectionID_DisplayOrder] ON [ProductSection](SectionID, DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductType]') AND name = N'IX_ProductType_ProductTypeGUID') CREATE INDEX [IX_ProductType_ProductTypeGUID] ON [ProductType](ProductTypeGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductType]') AND name = N'IX_ProductType_Name') CREATE INDEX [IX_ProductType_Name] ON [ProductType](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductType]') AND name = N'IX_ProductType_DisplayOrder') CREATE INDEX [IX_ProductType_DisplayOrder] ON [ProductType](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductType]') AND name = N'IX_ProductType_DisplayOrder_Name') CREATE INDEX [IX_ProductType_DisplayOrder_Name] ON [ProductType](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariant]') AND name = N'UIX_ProductVariant_VariantID') CREATE UNIQUE INDEX [UIX_ProductVariant_VariantID] ON [ProductVariant](VariantGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariant]') AND name = N'IX_ProductVariant_ProductID') CREATE INDEX [IX_ProductVariant_ProductID] ON [ProductVariant](ProductID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariant]') AND name = N'IX_ProductVariant_SKUSuffix') CREATE INDEX [IX_ProductVariant_SKUSuffix] ON [ProductVariant](SKUSuffix);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariant]') AND name = N'IX_ProductVariant_ManufacturerPartNumber') CREATE INDEX [IX_ProductVariant_ManufacturerPartNumber] ON [ProductVariant](ManufacturerPartNumber);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariant]') AND name = N'IX_ProductVariant_Deleted') CREATE INDEX [IX_ProductVariant_Deleted] ON [ProductVariant](Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariant]') AND name = N'IX_ProductVariant_Published') CREATE INDEX [IX_ProductVariant_Published] ON [ProductVariant](Published);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariant]') AND name = N'IX_ProductVariant_Wholesale') CREATE INDEX [IX_ProductVariant_Wholesale] ON [ProductVariant](Wholesale);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariant]') AND name = N'IX_ProductVariant_Deleted_Published') CREATE INDEX [IX_ProductVariant_Deleted_Published] ON [ProductVariant](Deleted, Published);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariant]') AND name = N'IX_ProductVariant_Deleted_Wholesale') CREATE INDEX [IX_ProductVariant_Deleted_Wholesale] ON [ProductVariant](Deleted, Wholesale);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariant]') AND name = N'IX_ProductVariant_IsDefault') CREATE INDEX [IX_ProductVariant_IsDefault] ON [ProductVariant](IsDefault);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariant]') AND name = N'IX_ProductVariant_DisplayOrder') CREATE INDEX [IX_ProductVariant_DisplayOrder] ON [ProductVariant](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariant]') AND name = N'IX_ProductVariant_Name') CREATE INDEX [IX_ProductVariant_Name] ON [ProductVariant](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProductVariant]') AND name = N'IX_ProductVariant_DisplayOrder_Name') CREATE INDEX [IX_ProductVariant_DisplayOrder_Name] ON [ProductVariant](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Profile]') AND name = N'IX_Profile_CustomerGuid') CREATE INDEX [IX_Profile_CustomerGuid] ON [Profile](CustomerGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]') AND name = N'XMLIX_Promotions_PromotionRuleData') CREATE PRIMARY XML INDEX [XMLIX_Promotions_PromotionRuleData] ON [Promotions](PromotionRuleData)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]') AND name = N'XMLIX_Promotions_PromotionRuleData_Path') CREATE XML INDEX [XMLIX_Promotions_PromotionRuleData_Path] ON [Promotions](PromotionRuleData) USING XML INDEX XMLIX_Promotions_PromotionRuleData FOR PATH
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]') AND name = N'XMLIX_Promotions_PromotionDiscountData') CREATE PRIMARY XML INDEX [XMLIX_Promotions_PromotionDiscountData] ON [Promotions](PromotionDiscountData)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]') AND name = N'XMLIX_Promotions_PromotionDiscountData_Path') CREATE XML INDEX [XMLIX_Promotions_PromotionDiscountData_Path] ON [Promotions](PromotionDiscountData) USING XML INDEX XMLIX_Promotions_PromotionDiscountData FOR PATH
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[QuantityDiscount]') AND name = N'UIX_QuantityDiscount_QuantityDiscountGUID') CREATE UNIQUE INDEX [UIX_QuantityDiscount_QuantityDiscountGUID] ON [QuantityDiscount](QuantityDiscountGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[QuantityDiscount]') AND name = N'IX_QuantityDiscount_DisplayOrder') CREATE INDEX [IX_QuantityDiscount_DisplayOrder] ON [QuantityDiscount](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[QuantityDiscount]') AND name = N'IX_QuantityDiscount_DisplayOrder_Name') CREATE INDEX [IX_QuantityDiscount_DisplayOrder_Name] ON [QuantityDiscount](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[QuantityDiscount]') AND name = N'IX_QuantityDiscount_Name') CREATE INDEX [IX_QuantityDiscount_Name] ON [QuantityDiscount](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[QuantityDiscountTable]') AND name = N'UIX_QuantityDiscountTable_QuantityDiscountTableGUID') CREATE UNIQUE INDEX [UIX_QuantityDiscountTable_QuantityDiscountTableGUID] ON [QuantityDiscountTable](QuantityDiscountTableGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[QuantityDiscountTable]') AND name = N'IX_QuantityDiscountTable_QuantityDiscountTableID') CREATE INDEX [IX_QuantityDiscountTable_QuantityDiscountTableID] ON [QuantityDiscountTable](QuantityDiscountID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[QuantityDiscountTable]') AND name = N'IX_QuantityDiscountTable_LowQuantity_HighQuantity') CREATE INDEX [IX_QuantityDiscountTable_LowQuantity_HighQuantity] ON [QuantityDiscountTable](LowQuantity, HighQuantity);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Rating]') AND name = N'IX_Rating_FoundNotHelpful') CREATE INDEX [IX_Rating_FoundNotHelpful] ON [Rating](FoundNotHelpful);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Rating]') AND name = N'IX_Rating_CreatedOn') CREATE INDEX [IX_Rating_CreatedOn] ON [Rating](CreatedOn);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Rating]') AND name = N'IX_Rating') CREATE INDEX [IX_Rating] ON [Rating](HasComment);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Rating]') AND name = N'IX_Rating_ProductID') CREATE INDEX [IX_Rating_ProductID] ON [Rating](ProductID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Rating]') AND name = N'IX_Rating_CustomerID') CREATE INDEX [IX_Rating_CustomerID] ON [Rating](CustomerID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Rating]') AND name = N'IX_Rating_IsROTD') CREATE INDEX [IX_Rating_IsROTD] ON [Rating](IsROTD);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Rating]') AND name = N'IX_Rating_FoundHelpful') CREATE INDEX [IX_Rating_FoundHelpful] ON [Rating](FoundHelpful);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Rating]') AND name = N'IX_Rating_IsFilthy') CREATE INDEX [IX_Rating_IsFilthy] ON [Rating](IsFilthy);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[RatingCommentHelpfulness]') AND name = N'IX_RatingCommentHelpfulness_StoreID') CREATE INDEX [IX_RatingCommentHelpfulness_StoreID] ON [RatingCommentHelpfulness](StoreID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[RatingCommentHelpfulness]') AND name = N'IX_RatingCommentHelpfulness_VotingCustomerID') CREATE INDEX [IX_RatingCommentHelpfulness_VotingCustomerID] ON [RatingCommentHelpfulness](VotingCustomerID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[RatingCommentHelpfulness]') AND name = N'IX_RatingCommentHelpfulness_ProductID') CREATE INDEX [IX_RatingCommentHelpfulness_ProductID] ON [RatingCommentHelpfulness](ProductID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[RatingCommentHelpfulness]') AND name = N'IX_RatingCommentHelpfulness_RatingCustomerID') CREATE INDEX [IX_RatingCommentHelpfulness_RatingCustomerID] ON [RatingCommentHelpfulness](RatingCustomerID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[RatingCommentHelpfulness]') AND name = N'IX_RatingCommentHelpfulness_Helpful') CREATE INDEX [IX_RatingCommentHelpfulness_Helpful] ON [RatingCommentHelpfulness](Helpful);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SalesPrompt]') AND name = N'UIX_SalesPrompt_SalesPromptGUID') CREATE UNIQUE INDEX [UIX_SalesPrompt_SalesPromptGUID] ON [SalesPrompt](SalesPromptGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SalesPrompt]') AND name = N'IX_SalesPrompt_Deleted') CREATE INDEX [IX_SalesPrompt_Deleted] ON [SalesPrompt](Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SalesPrompt]') AND name = N'IX_SalesPrompt_Name') CREATE INDEX [IX_SalesPrompt_Name] ON [SalesPrompt](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Section]') AND name = N'UIX_Section_SectionGUID') CREATE UNIQUE INDEX [UIX_Section_SectionGUID] ON [Section](SectionGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Section]') AND name = N'IX_Section_ParentSectionID') CREATE INDEX [IX_Section_ParentSectionID] ON [Section](ParentSectionID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Section]') AND name = N'IX_Section_DisplayOrder') CREATE INDEX [IX_Section_DisplayOrder] ON [Section](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Section]') AND name = N'IX_Section_Name') CREATE INDEX [IX_Section_Name] ON [Section](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Section]') AND name = N'IX_Section_DisplayOrder_Name') CREATE INDEX [IX_Section_DisplayOrder_Name] ON [Section](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Section]') AND name = N'IX_Section_Published') CREATE INDEX [IX_Section_Published] ON [Section](Published);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Section]') AND name = N'IX_Section_Wholesale') CREATE INDEX [IX_Section_Wholesale] ON [Section](Wholesale);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Section]') AND name = N'IX_Section_Deleted') CREATE INDEX [IX_Section_Deleted] ON [Section](Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Section]') AND name = N'IX_Section_Deleted_Published') CREATE INDEX [IX_Section_Deleted_Published] ON [Section](Deleted, Published);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Section]') AND name = N'IX_Section_Deleted_Wholesale') CREATE INDEX [IX_Section_Deleted_Wholesale] ON [Section](Deleted, Wholesale);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingByProduct]') AND name = N'UIX_ShippingByProduct_ShippingByProductGUID') CREATE UNIQUE INDEX [UIX_ShippingByProduct_ShippingByProductGUID] ON [ShippingByProduct](ShippingByProductGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingByTotal]') AND name = N'IX_ShippingByTotal_ShippingMethodID_LowValue_HighValue') CREATE INDEX [IX_ShippingByTotal_ShippingMethodID_LowValue_HighValue] ON [ShippingByTotal](ShippingMethodID, LowValue, HighValue);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingByTotal]') AND name = N'IX_ShippingByTotal_RowGUID') CREATE INDEX [IX_ShippingByTotal_RowGUID] ON [ShippingByTotal](RowGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingByTotalByPercent]') AND name = N'IX_ShippingByTotalByPercent_ShippingMethodID_LowValue_HighValue') CREATE INDEX [IX_ShippingByTotalByPercent_ShippingMethodID_LowValue_HighValue] ON [ShippingByTotalByPercent](ShippingMethodID, LowValue, HighValue);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingByTotalByPercent]') AND name = N'IX_ShippingByTotalByPercent_RowGUID') CREATE INDEX [IX_ShippingByTotalByPercent_RowGUID] ON [ShippingByTotalByPercent](RowGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingByWeight]') AND name = N'IX_ShippingByWeight_ShippingMethodID_LowValue_HighValue') CREATE INDEX [IX_ShippingByWeight_ShippingMethodID_LowValue_HighValue] ON [ShippingByWeight](ShippingMethodID, LowValue, HighValue);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingByWeight]') AND name = N'IX_ShippingByWeight_RowGUID') CREATE INDEX [IX_ShippingByWeight_RowGUID] ON [ShippingByWeight](RowGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingCalculation]') AND name = N'UIX_ShippingCalculation_ShippingCalculationID') CREATE UNIQUE INDEX [UIX_ShippingCalculation_ShippingCalculationID] ON [ShippingCalculation](ShippingCalculationGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingCalculation]') AND name = N'IX_ShippingCalculation_DisplayOrder') CREATE INDEX [IX_ShippingCalculation_DisplayOrder] ON [ShippingCalculation](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingCalculation]') AND name = N'IX_ShippingCalculation_Name') CREATE INDEX [IX_ShippingCalculation_Name] ON [ShippingCalculation](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingCalculation]') AND name = N'IX_ShippingCalculation_DisplayOrder_Name') CREATE INDEX [IX_ShippingCalculation_DisplayOrder_Name] ON [ShippingCalculation](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingMethod]') AND name = N'UIX_ShippingMethod_ShippingMethodGUID') CREATE UNIQUE INDEX [UIX_ShippingMethod_ShippingMethodGUID] ON [ShippingMethod](ShippingMethodGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingMethod]') AND name = N'IX_ShippingMethod_IsRTShipping') CREATE INDEX [IX_ShippingMethod_IsRTShipping] ON [ShippingMethod](IsRTShipping);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingMethod]') AND name = N'IX_ShippingMethod_DisplayOrder') CREATE INDEX [IX_ShippingMethod_DisplayOrder] ON [ShippingMethod](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingTotalByZone]') AND name = N'IX_ShippingTotalByZone_RowGUID') CREATE INDEX [IX_ShippingTotalByZone_RowGUID] ON [ShippingTotalByZone](RowGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingTotalByZone]') AND name = N'IX_ShippingTotalByZone_ShippingZoneID_LowValue_HighValue') CREATE INDEX [IX_ShippingTotalByZone_ShippingZoneID_LowValue_HighValue] ON [ShippingTotalByZone](ShippingZoneID, LowValue, HighValue);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingWeightByZone]') AND name = N'IX_ShippingWeightByZone_RowGUID') CREATE INDEX [IX_ShippingWeightByZone_RowGUID] ON [ShippingWeightByZone](RowGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingWeightByZone]') AND name = N'IX_ShippingWeightByZone_ShippingZoneID_LowValue_HighValue') CREATE INDEX [IX_ShippingWeightByZone_ShippingZoneID_LowValue_HighValue] ON [ShippingWeightByZone](ShippingZoneID, LowValue, HighValue);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingZone]') AND name = N'UIX_ShippingZone_ShippingZoneGUID') CREATE UNIQUE INDEX [UIX_ShippingZone_ShippingZoneGUID] ON [ShippingZone](ShippingZoneGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingZone]') AND name = N'IX_ShippingZone_DisplayOrder') CREATE INDEX [IX_ShippingZone_DisplayOrder] ON [ShippingZone](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShippingZone]') AND name = N'IX_ShippingZone_Deleted') CREATE INDEX [IX_ShippingZone_Deleted] ON [ShippingZone](Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShoppingCart]') AND name = N'UIX_ShoppingCart_ShoppingCartRecGUID') CREATE UNIQUE INDEX [UIX_ShoppingCart_ShoppingCartRecGUID] ON [ShoppingCart](ShoppingCartRecGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShoppingCart]') AND name = N'IX_ShoppingCart_CustomerID') CREATE INDEX [IX_ShoppingCart_CustomerID] ON [ShoppingCart](CustomerID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShoppingCart]') AND name = N'IX_ShoppingCart_CustomerID_CartType') CREATE INDEX [IX_ShoppingCart_CustomerID_CartType] ON [ShoppingCart](CustomerID, CartType);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShoppingCart]') AND name = N'IX_ShoppingCart_ProductID') CREATE INDEX [IX_ShoppingCart_ProductID] ON [ShoppingCart](ProductID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShoppingCart]') AND name = N'IX_ShoppingCart_VariantID') CREATE INDEX [IX_ShoppingCart_VariantID] ON [ShoppingCart](VariantID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShoppingCart]') AND name = N'IX_ShoppingCart_ProductID_VariantID_ChosenColor_ChosenSize') CREATE INDEX [IX_ShoppingCart_ProductID_VariantID_ChosenColor_ChosenSize] ON [ShoppingCart](ProductID, VariantID, ChosenColor, ChosenSize);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShoppingCart]') AND name = N'IX_ShoppingCart_CreatedOn') CREATE INDEX [IX_ShoppingCart_CreatedOn] ON [ShoppingCart](CreatedOn);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShoppingCart]') AND name = N'IX_ShoppingCart_CartType') CREATE INDEX [IX_ShoppingCart_CartType] ON [ShoppingCart](CartType);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShoppingCart]') AND name = N'IX_ShoppingCart_CartType_RecurringSubscriptionID') CREATE INDEX [IX_ShoppingCart_CartType_RecurringSubscriptionID] ON [ShoppingCart](CartType, RecurringSubscriptionID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShoppingCart]') AND name = N'IX_ShoppingCart_NextRecurringShipDate') CREATE INDEX [IX_ShoppingCart_NextRecurringShipDate] ON [ShoppingCart](NextRecurringShipDate);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ShoppingCart]') AND name = N'IX_ShoppingCart_RecurringIndex') CREATE INDEX [IX_ShoppingCart_RecurringIndex] ON [ShoppingCart](RecurringIndex);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[State]') AND name = N'UIX_State_StateGUID') CREATE UNIQUE INDEX [UIX_State_StateGUID] ON [State](StateGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[State]') AND name = N'UIX_State_Country_Abbrv') CREATE UNIQUE INDEX [UIX_State_Country_Abbrv] ON [State](CountryID, Abbreviation);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[State]') AND name = N'IX_State_DisplayOrder') CREATE INDEX [IX_State_DisplayOrder] ON [State](DisplayOrder);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[State]') AND name = N'IX_State_Name') CREATE INDEX [IX_State_Name] ON [State](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[State]') AND name = N'IX_State_DisplayOrder_Name') CREATE INDEX [IX_State_DisplayOrder_Name] ON [State](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[State]') AND name = N'IX_State_CountryID') CREATE INDEX [IX_State_CountryID] ON [State](CountryID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[StateTaxRate]') AND name = N'UIX_StateTaxRate_StateID_TaxClassID') CREATE UNIQUE INDEX [UIX_StateTaxRate_StateID_TaxClassID] ON [StateTaxRate](StateID, TaxClassID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[StringResource]') AND name = N'UIX_StringResource_Name_LocaleSetting_StoreId') CREATE UNIQUE INDEX [UIX_StringResource_Name_LocaleSetting_StoreId] ON [StringResource](Name, LocaleSetting, StoreID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Topic]') AND name = N'UIX_Topic_TopicGUID') CREATE UNIQUE INDEX [UIX_Topic_TopicGUID] ON [Topic](TopicGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Topic]') AND name = N'IX_Topic_Deleted') CREATE INDEX [IX_Topic_Deleted] ON [Topic](Deleted);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Topic]') AND name = N'IX_Topic_ShowInSiteMap') CREATE INDEX [IX_Topic_ShowInSiteMap] ON [Topic](ShowInSiteMap);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Vector]') AND name = N'UIX_Vector_VectorGUID') CREATE UNIQUE INDEX [UIX_Vector_VectorGUID] ON [Vector](VectorGUID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Vector]') AND name = N'IX_Vector_Name') CREATE INDEX [IX_Vector_Name] ON [Vector](Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Vector]') AND name = N'IX_Vector_DisplayOrder_Name') CREATE INDEX [IX_Vector_DisplayOrder_Name] ON [Vector](DisplayOrder, Name);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ZipTaxRate]') AND name = N'UIX_ZipTaxRate_ZipCode_TaxClassID_CountryID') CREATE UNIQUE INDEX [UIX_ZipTaxRate_ZipCode_TaxClassID_CountryID] ON [ZipTaxRate](ZipCode, TaxClassID, CountryID);

END
GO

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_GetProducts]'))
	DROP PROC [dbo].[aspdnsf_GetProducts]
GO

CREATE PROCEDURE [dbo].[aspdnsf_GetProducts]
    @categoryID      int = null,
    @sectionID       int = null,
    @manufacturerID  int = null,
    @distributorID   int = null,
    @genreID         int = null,
    @vectorID        int = null,
    @localeID        int = null,
    @CustomerLevelID int = null,
    @affiliateID     int = null,
    @ProductTypeID   int = null,
    @ViewType        bit = 1, -- 0 = all variants, 1 = one variant
    @sortEntity      int = 0, -- 1 = category, 2 = section, 3 = manufacturer, 4 = distributor, 5= genre, 6 = vector
    @pagenum         int = 1,
    @pagesize        int = null,
    @StatsFirst      tinyint = 1,
    @searchstr       nvarchar(4000) = null,
    @extSearch       tinyint = 0,
    @publishedonly   tinyint = 0,
    @ExcludeKits     tinyint = 0,
    @ExcludeSysProds tinyint = 0,
    @InventoryFilter int = 0,  --  will only show products with an inventory level GREATER OR EQUAL TO than the number specified in this parameter, set to -1 to disable inventory filtering
    @sortEntityName  varchar(20) = '', -- usely only when the entity id is provided, allowed values: category, section, manufacturer, distributor, genre, vector
    @localeName      varchar(20) = '',
    @OnSaleOnly      tinyint = 0,
	@storeID		 int = 1,
	@filterProduct	 bit = 0,
	@sortby			 varchar(10) = 'default',
	@since			 int = 180  -- best sellers in the last "@since" number of days
AS
BEGIN
	SET NOCOUNT ON

    DECLARE @rcount int
    DECLARE @productfiltersort table (rownum int not null identity  primary key, productid int not null, price money null, saleprice money null, displayorder int not null, VariantID int not null, VariantDisplayOrder int not null, ProductName nvarchar(400) null, VariantName nvarchar(400) null)
    DECLARE @productfilter table (rownum int not null identity  primary key, productid int not null, price money null, saleprice money null,  displayorder int not null, VariantID int not null, VariantDisplayOrder int not null, ProductName nvarchar(400) null, VariantName nvarchar(400) null)
	DECLARE @FilterProductsByAffiliate tinyint, @FilterProductsByCustomerLevel tinyint, @HideProductsWithLessThanThisInventoryLevel int
    CREATE TABLE #displayorder ([name] nvarchar (800), productid int not null primary key, displayorder int not null)
    CREATE TABLE #inventoryfilter (productid int not null, variantid int not null, InvQty int not null)
    CREATE CLUSTERED INDEX tmp_inventoryfilter ON #inventoryfilter (productid, variantid)

    DECLARE @customerLevelMappingsExist bit, @sectionMappingsExist bit, @localeMappingsExist bit, @affiliateMappingsExist bit, @categoryMappingsExist bit, @CustomerLevelFilteringIsAscending bit, @distributorMappingsExist bit, @genreMappingsExist bit, @vectorMappingsExist bit, @manufacturerMappingsExist bit, @ftsenabled tinyint = 0, @searching bit = 0

	IF @searchstr IS NOT NULL
		SET @searching = 1

	IF @searching = 1
	BEGIN
		IF ((SELECT DATABASEPROPERTYEX(db_name(db_id()),'IsFulltextEnabled')) = 1
			AND EXISTS (SELECT * FROM sysobjects WHERE id = OBJECT_ID(N'[dbo].[KeyWordSearch]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
			AND EXISTS (SELECT * FROM sysobjects WHERE id = OBJECT_ID(N'[dbo].[GetValidSearchString]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT')))
		BEGIN
			SET @ftsenabled = 1
		END
	END

    SET @FilterProductsByAffiliate = (SELECT TOP 1 case ConfigValue when 'true' then 1 else 0 end FROM dbo.AppConfig with (nolock) WHERE [Name] = 'FilterProductsByAffiliate' AND (StoreID=@storeID OR StoreID=0) ORDER BY StoreID desc)
    SET @FilterProductsByCustomerLevel = (SELECT TOP 1 case ConfigValue when 'true' then 1 else 0 end FROM dbo.AppConfig with (nolock) WHERE [Name] = 'FilterProductsByCustomerLevel' AND (StoreID=@storeID OR StoreID=0) ORDER BY StoreID desc)
    SET @HideProductsWithLessThanThisInventoryLevel = (SELECT TOP 1 case ConfigValue when -1 then 0 else ConfigValue end FROM dbo.AppConfig with (nolock) WHERE [Name] = 'HideProductsWithLessThanThisInventoryLevel' AND (StoreID=@storeID OR StoreID=0) ORDER BY StoreID desc)

    IF @InventoryFilter <> -1 and (@HideProductsWithLessThanThisInventoryLevel > @InventoryFilter or @HideProductsWithLessThanThisInventoryLevel  = -1)
        SET @InventoryFilter  = @HideProductsWithLessThanThisInventoryLevel

    SET @categoryID      = nullif(@categoryID, 0)
    SET @sectionID       = nullif(@sectionID, 0)
    SET @manufacturerID  = nullif(@manufacturerID, 0)
    SET @distributorID   = nullif(@distributorID, 0)
    SET @genreID         = nullif(@genreID, 0)
    SET @vectorID        = nullif(@vectorID, 0)
    SET @affiliateID     = nullif(@affiliateID, 0)
    SET @ProductTypeID   = nullif(@ProductTypeID, 0)

    SET @CustomerLevelFilteringIsAscending  = 0
    SET @CustomerLevelFilteringIsAscending = (SELECT TOP 1 case ConfigValue when 'true' then 1 else 0 end FROM dbo.AppConfig with (nolock) WHERE [Name] = 'FilterByCustomerLevelIsAscending' AND (StoreID=@storeID OR StoreID=0) ORDER BY StoreID desc)

    IF @localeID is null and ltrim(rtrim(@localeName)) <> ''
        SELECT @localeID = LocaleSettingID FROM dbo.LocaleSetting with (nolock) WHERE Name = ltrim(rtrim(@localeName))

	SELECT @categoryMappingsExist		= CASE WHEN EXISTS (SELECT * FROM ProductCategory) THEN 1 ELSE 0 END
	SELECT @sectionMappingsExist		= CASE WHEN EXISTS (SELECT * FROM ProductSection) THEN 1 ELSE 0 END
	SELECT @localeMappingsExist			= CASE WHEN EXISTS (SELECT * FROM ProductLocaleSetting) THEN 1 ELSE 0 END
	SELECT @customerLevelMappingsExist	= CASE WHEN EXISTS (SELECT * FROM ProductCustomerLevel) THEN 1 ELSE 0 END
	SELECT @affiliateMappingsExist		= CASE WHEN EXISTS (SELECT * FROM ProductAffiliate) THEN 1 ELSE 0 END
	SELECT @distributorMappingsExist	= CASE WHEN EXISTS (SELECT * FROM ProductDistributor) THEN 1 ELSE 0 END
	SELECT @genreMappingsExist			= CASE WHEN EXISTS (SELECT * FROM ProductGenre) THEN 1 ELSE 0 END
	SELECT @vectorMappingsExist			= CASE WHEN EXISTS (SELECT * FROM ProductVector) THEN 1 ELSE 0 END
	SELECT @manufacturerMappingsExist	= CASE WHEN EXISTS (SELECT * FROM ProductManufacturer) THEN 1 ELSE 0 END

    -- get page size
    IF @pagesize is null or @pagesize = 0 BEGIN
        IF @categoryID is not null
            SELECT @pagesize = PageSize FROM dbo.Category with (nolock) WHERE categoryID = @categoryID
        ELSE IF @sectionID is not null
            SELECT @pagesize = PageSize FROM dbo.Section with (nolock) WHERE sectionID = @sectionID
        ELSE IF @manufacturerID is not null
            SELECT @pagesize = PageSize FROM dbo.Manufacturer with (nolock) WHERE manufacturerID = @manufacturerID
        ELSE IF @distributorID is not null
            SELECT @pagesize = PageSize FROM dbo.Distributor with (nolock) WHERE distributorID = @distributorID
        ELSE IF @genreID is not null
            SELECT @pagesize = PageSize FROM dbo.Genre with (nolock) WHERE genreID = @genreID
        ELSE IF @vectorID is not null
            SELECT @pagesize = PageSize FROM dbo.Vector with (nolock) WHERE vectorID = @vectorID
        ELSE
            SET @pagesize = (SELECT TOP 1 ConfigValue FROM dbo.AppConfig WITH (NOLOCK) WHERE Name = 'Default_CategoryPageSize' AND (StoreID = @storeID OR StoreID = 0) ORDER BY StoreID DESC)
    END

    IF @pagesize is null or @pagesize = 0
        SET @pagesize = 20

    -- get sort order
    IF @filterProduct = 1 BEGIN
		IF @sortEntity = 1 or @sortEntityName = 'category' BEGIN
			INSERT #displayorder select distinct null as [name], a.productid, displayorder from dbo.ProductCategory a with (nolock) inner join (select distinct a.ProductID from ProductCategory a with (nolock)
			inner join ProductStore ps with (nolock) on a.ProductID = ps.ProductID and StoreID = @storeID) b  on a.ProductID = b.ProductID where categoryID = @categoryID
		END
		ELSE IF @sortEntity = 2 or @sortEntityName = 'section' BEGIN
			INSERT #displayorder select distinct null as [name], a.productid, displayorder from dbo.ProductSection a with (nolock) inner join (select distinct a.ProductID from ProductSection a with (nolock)
			inner join ProductStore ps with (nolock) on a.ProductID = ps.ProductID and StoreID = @storeID) b on a.ProductID = B.ProductID where sectionId = @sectionID
		END
		ELSE IF @sortEntity = 3 or @sortEntityName = 'manufacturer' BEGIN
			INSERT #displayorder select distinct null as [name], a.productid, displayorder from dbo.ProductManufacturer a with (nolock) inner join (select distinct a.ProductID from ProductManufacturer a with (nolock)
			inner join ProductStore ps with (nolock) on a.ProductID = ps.ProductID and StoreID = @storeID) b on a.ProductID = B.ProductID where ManufacturerID = @manufacturerID
		END
		ELSE IF @sortEntity = 4 or @sortEntityName = 'distributor' BEGIN
			INSERT #displayorder select distinct null as [name], a.productid, displayorder from dbo.ProductDistributor a with (nolock) inner join (select distinct a.ProductID from ProductDistributor a with (nolock)
			inner join ProductStore ps with (nolock) on a.ProductID = ps.ProductID and StoreID = @storeID) b on a.ProductID = B.ProductID where DistributorID = @distributorID
		END
		ELSE IF @sortEntity = 5 or @sortEntityName = 'genre' BEGIN
			INSERT #displayorder select distinct null as [name], a.productid, displayorder from dbo.ProductGenre a with (nolock) inner join (select distinct a.ProductID from ProductGenre a with (nolock)
			inner join ProductStore ps with (nolock) on a.ProductID = ps.ProductID and StoreID = @storeID) b on a.ProductID = B.ProductID where GenreID = @genreID
		END
		ELSE IF @sortEntity = 6 or @sortEntityName = 'vector' BEGIN
			INSERT #displayorder select distinct null as [name], a.productid, displayorder from dbo.ProductVector a with (nolock) inner join (select distinct a.ProductID from ProductVector a with (nolock)
			inner join ProductStore ps with (nolock) on a.ProductID = ps.ProductID and StoreID = @storeID) b on a.ProductID = B.ProductID where VectorID = @vectorID
		END
		ELSE BEGIN
			INSERT #displayorder select distinct [name], a.productid, 1 from dbo.Product a with (nolock) inner join (select distinct a.ProductID from Product a with (nolock)
			inner join ProductStore ps with (nolock) on a.ProductID = ps.ProductID and StoreID = @storeID) b on a.ProductID = B.ProductID ORDER BY Name
		END
	END
	ELSE BEGIN
		IF @sortEntity = 1 or @sortEntityName = 'category' BEGIN
			INSERT #displayorder select distinct null as [name], a.productid, displayorder from dbo.ProductCategory a with (nolock) where categoryID = @categoryID
		END
		ELSE IF @sortEntity = 2 or @sortEntityName = 'section' BEGIN
			INSERT #displayorder select distinct null as [name], a.productid, displayorder from dbo.ProductSection a with (nolock) where sectionId = @sectionID
		END
		ELSE IF @sortEntity = 3 or @sortEntityName = 'manufacturer' BEGIN
			INSERT #displayorder select distinct null as [name], a.productid, displayorder from dbo.ProductManufacturer a with (nolock) where ManufacturerID = @manufacturerID
		END
		ELSE IF @sortEntity = 4 or @sortEntityName = 'distributor' BEGIN
			INSERT #displayorder select distinct null as [name], a.productid, displayorder from dbo.ProductDistributor a with (nolock) where DistributorID = @distributorID
		END
		ELSE IF @sortEntity = 5 or @sortEntityName = 'genre' BEGIN
			INSERT #displayorder select distinct null as [name], a.productid, displayorder from dbo.ProductGenre a with (nolock) where GenreID = @genreID
		END
		ELSE IF @sortEntity = 6 or @sortEntityName = 'vector' BEGIN
			INSERT #displayorder select distinct null as [name], a.productid, displayorder from dbo.ProductVector a with (nolock) where VectorID = @vectorID
		END
		ELSE BEGIN
			INSERT #displayorder select distinct [name], a.productid, 1 from dbo.Product a with (nolock) ORDER BY Name
		END
	END

	IF @searching = 1
	BEGIN
		IF (@ftsenabled = 1)
		BEGIN
			IF rtrim(isnull(@searchstr, '')) <> ''
			BEGIN
				DECLARE @tmpsrch nvarchar(4000)
				SET @tmpsrch = dbo.GetValidSearchString(@searchstr)
				DELETE #displayorder from #displayorder d left join dbo.KeyWordSearch(@tmpsrch) k on d.productid = k.productid where k.productid is null
			END
		END

		SET @searchstr = '%' + rtrim(ltrim(@searchstr)) + '%'
	END

    IF @InventoryFilter <> -1 BEGIN
        IF @ViewType = 1 BEGIN
            INSERT #inventoryfilter
            SELECT p.productid, pv.VariantID, sum(case p.TrackInventoryBySizeAndColor when 1 then isnull(i.quan, 0) else pv.inventory end ) invqty
            FROM product p with (NOLOCK) join #displayorder d on p.ProductID = d.ProductID
                join ProductVariant pv with (NOLOCK) on p.ProductID = pv.ProductID  and pv.IsDefault = 1
                left join Inventory i with (NOLOCK) on pv.VariantID = i.VariantID
            GROUP BY p.productid, pv.VariantID
            HAVING sum(case p.TrackInventoryBySizeAndColor when 1 then isnull(i.quan, 0) else pv.inventory end ) >= @InventoryFilter
        END
        ELSE
            INSERT #inventoryfilter
            SELECT p.productid, pv.VariantID, sum(case p.TrackInventoryBySizeAndColor when 1 then isnull(i.quan, 0) else pv.inventory end ) invqty
            FROM product p with (NOLOCK) join #displayorder d on p.ProductID = d.ProductID
                join ProductVariant pv with (NOLOCK) on p.ProductID = pv.ProductID
                left join Inventory i with (NOLOCK) on pv.VariantID = i.VariantID
            GROUP BY p.productid, pv.VariantID
            HAVING sum(case p.TrackInventoryBySizeAndColor when 1 then isnull(i.quan, 0) else pv.inventory end ) >= @InventoryFilter


        INSERT @productfilter (productid, displayorder, VariantID, VariantDisplayOrder, ProductName, VariantName)
        SELECT distinct p.productid, do.displayorder, pv.VariantID, pv.DisplayOrder, p.Name, pv.Name
        FROM
            product p with (nolock)
            join #displayorder do on p.ProductID = do.ProductID
            left join ProductVariant pv        with (NOLOCK) ON p.ProductID = pv.ProductID and pv.IsDefault >= @ViewType
            left join productcategory pc       with (nolock) on p.ProductID = pc.ProductID
            left join productsection ps        with (nolock) on p.ProductID = ps.ProductID
            left join ProductManufacturer pm   with (nolock) on p.ProductID = pm.ProductID
            left join ProductDistributor pd    with (nolock) on p.ProductID = pd.ProductID
            left join ProductGenre px          with (nolock) on p.ProductID = px.ProductID
            left join ProductVector px2        with (nolock) on p.ProductID = px2.ProductID
            left join ProductLocaleSetting pl  with (nolock) on p.ProductID = pl.ProductID
            left join ProductCustomerLevel pcl with (nolock) on p.ProductID = pcl.ProductID
            left join ProductAffiliate pa      with (nolock) on p.ProductID = pa.ProductID

            join #inventoryfilter i on pv.VariantID = i.VariantID
        WHERE
              (pc.categoryid = @categoryID or @categoryID is null or @categoryMappingsExist = 0)
          and (ps.sectionid = @sectionID or @sectionID is null or @sectionMappingsExist = 0)
          and (pl.LocaleSettingID = @localeID or @localeID is null or @localeMappingsExist = 0)
          and (pa.AffiliateID = @affiliateID or pa.AffiliateID is null or @affiliateMappingsExist = 0 or @FilterProductsByAffiliate = 0)
          and (pm.manufacturerid = @manufacturerID or @manufacturerID is null or @manufacturerMappingsExist = 0)
          and (pd.DistributorID = @distributorID or @distributorID is null or @distributorMappingsExist = 0)
          and (px.GenreID = @genreID or @genreID is null or @genreMappingsExist = 0)
          and (px2.VectorID = @vectorID or @vectorID is null or @vectorMappingsExist = 0)
          and p.ProductTypeID = coalesce(@ProductTypeID, p.ProductTypeID)
          and (case
                when @FilterProductsByCustomerLevel = 0 or @customerLevelMappingsExist = 0 or pcl.CustomerLevelID is null or @CustomerLevelID is null then 1
                when @CustomerLevelFilteringIsAscending = 1 and pcl.CustomerLevelID <= @CustomerLevelID then 1
                when pcl.CustomerLevelID = @CustomerLevelID or pcl.CustomerLevelID is null then 1
                else 0
               end  = 1
              )
          and (@ftsenabled = 1 or
					(@searching = 0 or
						(@searching = 1 and
							(@ftsenabled = 0 and
								(patindex(@searchstr, isnull(p.name, '')) > 0
									or patindex(@searchstr, isnull(convert(nvarchar(20),p.productid), '')) > 0
									or patindex(@searchstr, isnull(pv.name, '')) > 0
									or patindex(@searchstr, isnull(p.sku , '')+isnull(pv.skusuffix , '')) > 0
									or patindex(@searchstr, isnull(p.manufacturerpartnumber, '')) > 0
									or patindex(@searchstr, isnull(pv.manufacturerpartnumber, '')) > 0
									or (patindex(@searchstr, isnull(p.Description, '')) > 0 and @extSearch = 1)
									or (patindex(@searchstr, isnull(p.Summary, '')) > 0 and @extSearch = 1)
								)
							)
						)
					)
				)
          and case when isnull(pv.saleprice,0) = 0 then 0 else 1 end >= @OnSaleOnly
          and p.published >= @publishedonly
          and pv.published >= @publishedonly
          and isnull(p.IsAKit, 0) <= 1-@ExcludeKits
          and p.IsSystem <= 1-@ExcludeSysProds
          and p.Deleted = 0
          and pv.Deleted = 0
        order by do.displayorder, p.Name, pv.DisplayOrder, pv.Name

    END
    ELSE BEGIN
        INSERT @productfilter (productid, displayorder, VariantID, VariantDisplayOrder, ProductName, VariantName)
        SELECT distinct p.productid, do.displayorder, pv.VariantID, pv.DisplayOrder, p.Name, pv.Name
        FROM
            product p with (nolock)
            join #displayorder do on p.ProductID = do.ProductID
            join ProductVariant pv             with (NOLOCK) on p.ProductID = pv.ProductID and pv.IsDefault >= @ViewType
            left join productcategory pc       with (nolock) on p.ProductID = pc.ProductID
            left join productsection ps        with (nolock) on p.ProductID = ps.ProductID
            left join ProductManufacturer pm   with (nolock) on p.ProductID = pm.ProductID
            left join ProductDistributor pd    with (nolock) on p.ProductID = pd.ProductID
            left join ProductGenre px          with (nolock) on p.ProductID = px.ProductID
            left join ProductVector px2        with (nolock) on p.ProductID = px2.ProductID
            left join ProductLocaleSetting pl  with (nolock) on p.ProductID = pl.ProductID
            left join ProductCustomerLevel pcl with (nolock) on p.ProductID = pcl.ProductID
            left join ProductAffiliate pa      with (nolock) on p.ProductID = pa.ProductID
        WHERE
              (pc.categoryid = @categoryID or @categoryID is null or @categoryMappingsExist = 0)
          and (ps.sectionid = @sectionID or @sectionID is null or @sectionMappingsExist = 0)
          and (pl.LocaleSettingID = @localeID or @localeID is null or @localeMappingsExist = 0)
          and (pa.AffiliateID = @affiliateID or pa.AffiliateID is null or @affiliateMappingsExist = 0 or @FilterProductsByAffiliate = 0)
          and (pm.manufacturerid = @manufacturerID or @manufacturerID is null or @manufacturerMappingsExist = 0)
          and (pd.DistributorID = @distributorID or @distributorID is null or @distributorMappingsExist = 0)
          and (px.GenreID = @genreID or @genreID is null or @genreMappingsExist = 0)
          and (px2.VectorID = @vectorID or @vectorID is null or @vectorMappingsExist = 0)
          and p.ProductTypeID = coalesce(@ProductTypeID, p.ProductTypeID)
          and (case
                when @FilterProductsByCustomerLevel = 0 or @customerLevelMappingsExist = 0 or pcl.CustomerLevelID is null or @CustomerLevelID is null then 1
                when @CustomerLevelFilteringIsAscending = 1 and pcl.CustomerLevelID <= @CustomerLevelID then 1
                when pcl.CustomerLevelID = @CustomerLevelID or pcl.CustomerLevelID is null then 1
                else 0
               end  = 1
              )
          and (@ftsenabled = 1 or
					(@searching = 0 or
						(@searching = 1 and
							(@ftsenabled = 0 and
								(patindex(@searchstr, isnull(p.name, '')) > 0
									or patindex(@searchstr, isnull(convert(nvarchar(20),p.productid), '')) > 0
									or patindex(@searchstr, isnull(pv.name, '')) > 0
									or patindex(@searchstr, isnull(p.sku , '')+isnull(pv.skusuffix , '')) > 0
									or patindex(@searchstr, isnull(p.manufacturerpartnumber, '')) > 0
									or patindex(@searchstr, isnull(pv.manufacturerpartnumber, '')) > 0
									or (patindex(@searchstr, isnull(p.Description, '')) > 0 and @extSearch = 1)
									or (patindex(@searchstr, isnull(p.Summary, '')) > 0 and @extSearch = 1)
								)
							)
						)
					)
				)
          and case when isnull(pv.saleprice,0) = 0 then 0 else 1 end >= @OnSaleOnly
          and p.published >= @publishedonly
          and pv.published >= @publishedonly
          and isnull(p.IsAKit, 0) <= 1-@ExcludeKits
          and p.IsSystem <= 1-@ExcludeSysProds
          and p.Deleted = 0
          and pv.Deleted = 0
        order by do.displayorder, p.Name, pv.DisplayOrder, pv.Name
    END

    SET @rcount = @@rowcount
    IF @StatsFirst = 1
        SELECT cast(ceiling(@rcount*1.0/@pagesize) as int) pages, @rcount ProductCount

  --Begin sorting
  	if @sortby = 'bestseller'
		begin
			insert @productfiltersort (productid, price, saleprice, displayorder, VariantID, VariantDisplayOrder, ProductName, VariantName)
			select pf.productid, pf.price, pf.saleprice, pf.displayorder, pf.VariantID, pf.VariantDisplayOrder, pf.ProductName, pf.VariantName
				from @productfilter pf
				inner join (
					select ProductID, SUM(Quantity) AS NumSales
					  from dbo.Orders_ShoppingCart sc with (NOLOCK)
							join [dbo].Orders o with (NOLOCK)  on sc.OrderNumber = o.OrderNumber and o.OrderDate >= dateadd(dy, -@since, getdate())
					  group by ProductID
				) bsSort on pf.productid = bsSort.ProductID
				order by isnull(bsSort.NumSales, 0) DESC
		end
  	else --default
		begin
			insert @productfiltersort (productid, price, saleprice, displayorder, VariantID, VariantDisplayOrder, ProductName, VariantName)
			select productid, price, saleprice, displayorder, VariantID, VariantDisplayOrder, ProductName, VariantName
			from @productfilter order by displayorder, productName, variantDisplayOrder, variantName
		end

    -- Check filtered products for recurring variants
    Declare @ProductResults Table
    (
		ProductID int
		, VariantID int
		, HasRecurring bit
		, RowNum int
    );
    -- temp table based on filtered product result set
    Insert Into @ProductResults

    SELECT   Distinct
        p.ProductID,
		pv.VariantID,
		0,
		pf.rownum
    FROM dbo.Product p with (NOLOCK)
        left join dbo.ProductVariant       pv  with (NOLOCK) on p.ProductID = pv.ProductID and pv.IsDefault >= @ViewType
        join @productfiltersort            pf                on pv.ProductID = pf.ProductID and pv.VariantID = pf.VariantID
    WHERE pf.rownum >= @pagesize*(@pagenum-1)+1 and pf.rownum <= @pagesize*(@pagenum)
    ORDER BY pf.rownum

    -- set HasRecurring
    Update pr
    Set HasRecurring = 1
    From @ProductResults pr, (
		Select prs.ProductId
		From @ProductResults prs, ProductVariant pv
		Where prs.ProductID = pv.ProductID
		And pv.IsRecurring = 1
		And pv.Deleted = 0
		And pv.Published = 1
		Group By prs.ProductId
		Having Count(*) > 0) tmp
    Where pr.ProductId = tmp.ProductId
    ---- End Recurring

    SELECT
        p.ProductID,
        p.Name,
        pv.VariantID,
        pv.Name AS VariantName,
        p.ProductGUID,
        p.Summary,
        p.Description,
        p.SEKeywords,
        p.SEDescription,
        p.MiscText,
        p.FroogleDescription,
        p.SETitle,
        p.SEAltText,
        p.SizeOptionPrompt,
        p.ColorOptionPrompt,
        p.TextOptionPrompt,
        p.ProductTypeID,
        p.TaxClassID,
        p.SKU,
        p.ManufacturerPartNumber,
        p.SalesPromptID,
        p.IsFeatured,
        p.XmlPackage,
        p.ColWidth,
        p.Published,
        p.RequiresRegistration,
        p.Looks,
        p.Notes,
        p.QuantityDiscountID,
        p.RelatedProducts,
        p.UpsellProducts,
        p.UpsellProductDiscountPercentage,
        p.RelatedDocuments,
        p.TrackInventoryBySizeAndColor,
        p.TrackInventoryBySize,
        p.TrackInventoryByColor,
        p.IsAKit,
        p.ShowBuyButton,
        p.RequiresProducts,
        p.HidePriceUntilCart,
        p.IsCalltoOrder,
        p.ExcludeFromPriceFeeds,
        p.RequiresTextOption,
        p.TextOptionMaxLength,
        p.SEName,
        p.Deleted,
        p.CreatedOn,
        p.ImageFilenameOverride,
        pv.VariantGUID,
        pv.Description AS VariantDescription,
        pv.SEKeywords AS VariantSEKeywords,
        pv.SEDescription AS VariantSEDescription,
        pv.Colors,
        pv.ColorSKUModifiers,
        pv.Sizes,
        pv.SizeSKUModifiers,
        pv.FroogleDescription AS VariantFroogleDescription,
        pv.SKUSuffix,
        pv.ManufacturerPartNumber AS VariantManufacturerPartNumber,
        pv.Price,
        pv.CustomerEntersPrice,
        pv.CustomerEntersPricePrompt,
        isnull(pv.SalePrice, 0) SalePrice,
        cast(isnull(pv.Weight,0) as decimal(10,1)) Weight,
        pv.MSRP,
        pv.Cost,
        isnull(pv.Points,0) Points,
        pv.Dimensions,
        case p.TrackInventoryBySizeAndColor when 1 then isnull(i.quan, 0) else pv.inventory end Inventory,
        pv.DisplayOrder as VariantDisplayOrder,
        pv.Notes AS VariantNotes,
        pv.IsTaxable,
        pv.IsShipSeparately,
        pv.IsDownload,
        pv.DownloadLocation,
        pv.Published AS VariantPublished,
        pv.IsSecureAttachment,
        pv.IsRecurring,
        pv.RecurringInterval,
        pv.RecurringIntervalType,
        pv.SEName AS VariantSEName,
        pv.RestrictedQuantities,
        pv.MinimumQuantity,
        pv.Deleted AS VariantDeleted,
        pv.CreatedOn AS VariantCreatedOn,
        d.Name AS DistributorName,
        d.DistributorID,
        d.SEName AS DistributorSEName,
        m.ManufacturerID,
        m.Name AS ManufacturerName,
        m.SEName AS ManufacturerSEName,
        s.Name AS SalesPromptName,
        pf.HasRecurring,
        case when pcl.productid is null then 0 else isnull(ep.Price, 0) end ExtendedPrice
    FROM dbo.Product p with (NOLOCK)
        left join dbo.ProductVariant       pv  with (NOLOCK) on p.ProductID = pv.ProductID and pv.IsDefault >= @ViewType
        inner join @ProductResults			   pf                on pv.ProductID = pf.ProductID and pv.VariantID = pf.VariantID
        left join dbo.SalesPrompt           s  with (NOLOCK) on p.SalesPromptID = s.SalesPromptID
        left join dbo.ProductManufacturer  pm  with (NOLOCK) on p.ProductID = pm.ProductID
        left join dbo.Manufacturer          m  with (NOLOCK) on pm.ManufacturerID = m.ManufacturerID
        left join dbo.ProductDistributor   pd  with (NOLOCK) on p.ProductID = pd.ProductID
        left join dbo.Distributor           d  with (NOLOCK) on pd.DistributorID = d.DistributorID
        left join dbo.ExtendedPrice        ep  with (NOLOCK) on ep.VariantID = pv.VariantID and ep.CustomerLevelID = @CustomerLevelID
        left join dbo.ProductCustomerLevel pcl with (NOLOCK) on p.ProductID = pcl.ProductID and pcl.CustomerLevelID = @CustomerLevelID
        left join (select VariantID, sum(quan) quan from dbo.Inventory with (nolock) group by VariantID) i  on pv.VariantID = i.VariantID
		order by pf.rownum

    IF @StatsFirst <> 1
        SELECT cast(ceiling(@rcount*1.0/@pagesize) as int) pages, @rcount ProductCount
END
GO

if not exists(select * from sys.procedures where Name = 'aspdnsf_DatabaseMaintenance' and type = 'P')
	EXEC sp_rename 'aspdnsf_MonthlyMaintenance', 'aspdnsf_DatabaseMaintenance'

if not exists(select * from AppConfig where Name = 'System.SavedMonthlyMaintenance')
	update appconfig set name = 'System.SavedDatabaseMaintenance' where name = 'System.SavedMonthlyMaintenance'

--Ditch unused T-HUB columns in the DB
if exists(select * from sys.columns where Name = 'THUB_POSTED_TO_ACCOUNTING' and [object_id] = object_id('dbo.Orders'))
begin
	alter table Orders drop constraint [DF_Orders_THUB_POSTED_TO_ACCOUNTING]
	ALTER TABLE Orders DROP COLUMN THUB_POSTED_TO_ACCOUNTING;
end

if exists(select * from sys.columns where Name = 'THUB_POSTED_DATE' and [object_id] = object_id('dbo.Orders'))
	ALTER TABLE Orders DROP COLUMN THUB_POSTED_DATE;

if exists(select * from sys.columns where Name = 'THUB_ACCOUNTING_REF' and [object_id] = object_id('dbo.Orders'))
	ALTER TABLE Orders DROP COLUMN THUB_ACCOUNTING_REF;

IF EXISTS (select * from dbo.sysobjects where id = object_id(N'[dbo].[aspdnsf_getOrder]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
	drop proc [dbo].[aspdnsf_getOrder]
GO

CREATE proc [dbo].[aspdnsf_getOrder]
	@ordernumber int
as
set nocount on
select
	o.OrderNumber,
	o.OrderGUID,
	o.ParentOrderNumber,
	o.StoreVersion,
	o.QuoteCheckout,
	o.IsNew,
	o.ShippedOn,
	o.CustomerID,
	o.CustomerGUID,
	o.Referrer,
	o.SkinID,
	o.LastName,
	o.FirstName,
	o.Email,
	o.Notes,
	o.BillingEqualsShipping,
	o.BillingLastName,
	o.BillingFirstName,
	o.BillingCompany,
	o.BillingAddress1,
	o.BillingAddress2,
	o.BillingSuite,
	o.BillingCity,
	o.BillingState,
	o.BillingZip,
	o.BillingCountry,
	o.BillingPhone,
	o.ShippingLastName,
	o.ShippingFirstName,
	o.ShippingCompany,
	o.ShippingResidenceType,
	o.ShippingAddress1,
	o.ShippingAddress2,
	o.ShippingSuite,
	o.ShippingCity,
	o.ShippingState,
	o.ShippingZip,
	o.ShippingCountry,
	o.ShippingMethodID,
	o.ShippingMethod,
	o.ShippingPhone,
	o.ShippingCalculationID,
	o.Phone,
	o.RegisterDate,
	o.AffiliateID,
	o.CouponCode,
	o.CouponType,
	o.CouponDescription,
	o.CouponDiscountAmount,
	o.CouponDiscountPercent,
	o.CouponIncludesFreeShipping,
	o.OkToEmail,
	o.Deleted,
	o.CardType,
	o.CardName,
	o.CardNumber,
	o.CardExpirationMonth,
	o.CardExpirationYear,
	o.OrderSubtotal,
	o.OrderTax,
	o.OrderShippingCosts,
	o.OrderTotal,
	o.PaymentGateway,
	o.AuthorizationCode,
	o.AuthorizationResult,
	o.AuthorizationPNREF,
	o.TransactionCommand,
	o.OrderDate,
	o.LevelID,
	o.LevelName,
	o.LevelDiscountPercent,
	o.LevelDiscountAmount,
	o.LevelHasFreeShipping,
	o.LevelAllowsQuantityDiscounts,
	o.LevelHasNoTax,
	o.LevelAllowsCoupons,
	o.LevelDiscountsApplyToExtendedPrices,
	o.LastIPAddress,
	o.PaymentMethod,
	o.OrderNotes,
	o.PONumber,
	o.DownloadEmailSentOn,
	o.ReceiptEmailSentOn,
	o.DistributorEmailSentOn,
	o.ShippingTrackingNumber,
	o.ShippedVIA,
	o.CustomerServiceNotes,
	o.RTShipRequest,
	o.RTShipResponse,
	o.TransactionState,
	o.AVSResult,
	o.CaptureTXCommand,
	o.CaptureTXResult,
	o.VoidTXCommand,
	o.VoidTXResult,
	o.RefundTXCommand,
	o.RefundTXResult,
	o.CardinalLookupResult,
	o.CardinalAuthenticateResult,
	o.CardinalGatewayParms,
	o.AffiliateCommissionRecorded,
	o.OrderOptions,
	o.OrderWeight,
	o.CarrierReportedRate,
	o.CarrierReportedWeight,
	o.LocaleSetting,
	o.FinalizationData,
	o.ExtensionData,
	o.AlreadyConfirmed,
	o.CartType,
	o.Last4,
	o.ReadyToShip,
	o.IsPrinted,
	o.AuthorizedOn,
	o.CapturedOn,
	o.RefundedOn,
	o.VoidedOn,
	o.EditedOn,
	o.InventoryWasReduced,
	o.MaxMindFraudScore,
	o.MaxMindDetails,
	o.CardStartDate,
	o.CardIssueNumber,
	o.TransactionType,
	o.Crypt,
	o.VATRegistrationID,
	o.FraudedOn,
	o.RefundReason,
	o.AuthorizationPNREF as TransactionID,
	o.RecurringSubscriptionID,
	o.RelatedOrderNumber,
	o.ReceiptHtml,
	os.ShoppingCartRecID,
	os.IsTaxable,
	os.IsShipSeparately,
	os.IsDownload,
	os.DownloadLocation,
	os.FreeShipping,
	os.DistributorID,
	os.ShippingDetail,
	os.TaxClassID,
	os.TaxRate,
	os.Notes as CartNotes,
	os.CustomerEntersPrice,
	os.ProductID,
	os.VariantID,
	os.Quantity,
	os.ChosenColor,
	os.ChosenColorSKUModifier,
	os.ChosenSize,
	os.ChosenSizeSKUModifier,
	os.TextOption,
	os.SizeOptionPrompt,
	os.ColorOptionPrompt,
	os.TextOptionPrompt,
	os.CustomerEntersPricePrompt,
	os.OrderedProductQuantityDiscountID,
	os.OrderedProductQuantityDiscountName,
	os.OrderedProductQuantityDiscountPercent,
	os.OrderedProductName,
	os.OrderedProductVariantName,
	os.OrderedProductSKU,
	os.OrderedProductManufacturerPartNumber,
	os.OrderedProductPrice,
	os.OrderedProductWeight,
	os.OrderedProductPrice,
	os.ShippingMethodID,
	os.ShippingMethodID CartItemShippingMethodID,
	os.ShippingMethod CartItemShippingMethod,
	os.ShippingAddressID,
	os.IsAKit
from Orders o with (nolock)
	left join orders_ShoppingCart os with (nolock) ON os.OrderNumber = o.OrderNumber
where o.OrderNumber = @ordernumber
order by os.ShippingAddressID
GO


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[aspdnsf_updOrders]'))
	DROP PROC [dbo].[aspdnsf_updOrders]
GO

create proc dbo.aspdnsf_updOrders
    @OrderNumber int,
    @ParentOrderNumber int = null,
    @StoreVersion nvarchar(100) = null,
    @QuoteCheckout tinyint = null,
    @IsNew tinyint = null,
    @ShippedOn datetime = null,
    @CustomerID int = null,
    @CustomerGUID uniqueidentifier = null,
    @Referrer nvarchar(max) = null,
    @SkinID int = null,
    @LastName nvarchar(100) = null,
    @FirstName nvarchar(100) = null,
    @Email nvarchar(100) = null,
    @Notes nvarchar(max) = null,
    @BillingEqualsShipping tinyint = null,
    @BillingLastName nvarchar(100) = null,
    @BillingFirstName nvarchar(100) = null,
    @BillingCompany nvarchar(100) = null,
    @BillingAddress1 nvarchar(100) = null,
    @BillingAddress2 nvarchar(100) = null,
    @BillingSuite nvarchar(50) = null,
    @BillingCity nvarchar(100) = null,
    @BillingState nvarchar(100) = null,
    @BillingZip nvarchar(10) = null,
    @BillingCountry nvarchar(100) = null,
    @BillingPhone nvarchar(25) = null,
    @ShippingLastName nvarchar(100) = null,
    @ShippingFirstName nvarchar(100) = null,
    @ShippingCompany nvarchar(100) = null,
    @ShippingResidenceType int = null,
    @ShippingAddress1 nvarchar(100) = null,
    @ShippingAddress2 nvarchar(100) = null,
    @ShippingSuite nvarchar(50) = null,
    @ShippingCity nvarchar(100) = null,
    @ShippingState nvarchar(100) = null,
    @ShippingZip nvarchar(10) = null,
    @ShippingCountry nvarchar(100) = null,
    @ShippingMethodID int = null,
    @ShippingMethod nvarchar(max) = null,
    @ShippingPhone nvarchar(25) = null,
    @ShippingCalculationID int = null,
    @Phone nvarchar(20) = null,
    @RegisterDate datetime = null,
    @AffiliateID int = null,
    @CouponCode nvarchar(50) = null,
    @CouponType int = null,
    @CouponDescription nvarchar(max) = null,
    @CouponDiscountAmount money = null,
    @CouponDiscountPercent money = null,
    @CouponIncludesFreeShipping tinyint = null,
    @OkToEmail tinyint = null,
    @Deleted tinyint = null,
    @CardType nvarchar(20) = null,
    @CardName nvarchar(100) = null,
    @CardNumber nvarchar(max) = null,
    @CardExpirationMonth nvarchar(10) = null,
    @CardExpirationYear nvarchar(10) = null,
    @OrderSubtotal money = null,
    @OrderTax money = null,
    @OrderShippingCosts money = null,
    @OrderTotal money = null,
    @PaymentGateway nvarchar(50) = null,
    @AuthorizationCode nvarchar(100) = null,
    @AuthorizationResult nvarchar(max) = null,
    @AuthorizationPNREF nvarchar(max) = null,
    @TransactionCommand nvarchar(max) = null,
    @OrderDate datetime = null,
    @LevelID int = null,
    @LevelName nvarchar(100) = null,
    @LevelDiscountPercent money = null,
    @LevelDiscountAmount money = null,
    @LevelHasFreeShipping tinyint = null,
    @LevelAllowsQuantityDiscounts tinyint = null,
    @LevelHasNoTax tinyint = null,
    @LevelAllowsCoupons tinyint = null,
    @LevelDiscountsApplyToExtendedPrices tinyint = null,
    @LastIPAddress varchar(40) = null,
    @PaymentMethod nvarchar(100) = null,
    @OrderNotes nvarchar(max) = null,
    @PONumber nvarchar(50) = null,
    @DownloadEmailSentOn datetime = null,
    @ReceiptEmailSentOn datetime = null,
    @DistributorEmailSentOn datetime = null,
    @ShippingTrackingNumber nvarchar(100) = null,
    @ShippedVIA nvarchar(100) = null,
    @CustomerServiceNotes nvarchar(max) = null,
    @RTShipRequest nvarchar(max) = null,
    @RTShipResponse nvarchar(max) = null,
    @TransactionState nvarchar(20) = null,
    @AVSResult nvarchar(50) = null,
    @CaptureTXCommand nvarchar(max) = null,
    @CaptureTXResult nvarchar(max) = null,
    @VoidTXCommand nvarchar(max) = null,
    @VoidTXResult nvarchar(max) = null,
    @RefundTXCommand nvarchar(max) = null,
    @RefundTXResult nvarchar(max) = null,
    @CardinalLookupResult nvarchar(max) = null,
    @CardinalAuthenticateResult nvarchar(max) = null,
    @CardinalGatewayParms nvarchar(max) = null,
    @AffiliateCommissionRecorded tinyint = null,
    @OrderOptions nvarchar(max) = null,
    @OrderWeight money = null,
    @CarrierReportedRate nvarchar(max) = null,
    @CarrierReportedWeight nvarchar(max) = null,
    @LocaleSetting nvarchar(10) = null,
    @FinalizationData nvarchar(max) = null,
    @ExtensionData nvarchar(max) = null,
    @AlreadyConfirmed tinyint = null,
    @CartType int = null,
    @Last4 nvarchar(4) = null,
    @ReadyToShip tinyint = null,
    @IsPrinted tinyint = null,
    @AuthorizedOn datetime = null,
    @CapturedOn datetime = null,
    @RefundedOn datetime = null,
    @VoidedOn datetime = null,
    @InventoryWasReduced int = null,
    @MaxMindFraudScore decimal(5, 2) = null,
    @MaxMindDetails nvarchar(max) = null,
    @CardStartDate nvarchar(20) = null,
    @CardIssueNumber nvarchar(25) = null,
    @TransactionType int = null,
    @Crypt int = null,
    @VATRegistrationID nvarchar(max) = null,
    @FraudedOn tinyint = null,
    @RefundReason nvarchar(max) = null
as
set nocount on

update dbo.Orders
set
	ParentOrderNumber = COALESCE(@ParentOrderNumber, ParentOrderNumber),
	StoreVersion = COALESCE(@StoreVersion, StoreVersion),
	QuoteCheckout = COALESCE(@QuoteCheckout, QuoteCheckout),
	IsNew = COALESCE(@IsNew, IsNew),
	ShippedOn = COALESCE(@ShippedOn, ShippedOn),
	CustomerID = COALESCE(@CustomerID, CustomerID),
	CustomerGUID = COALESCE(@CustomerGUID, CustomerGUID),
	Referrer = COALESCE(@Referrer, Referrer),
	SkinID = COALESCE(@SkinID, SkinID),
	LastName = COALESCE(@LastName, LastName),
	FirstName = COALESCE(@FirstName, FirstName),
	Email = COALESCE(@Email, Email),
	Notes = COALESCE(@Notes, Notes),
	BillingEqualsShipping = COALESCE(@BillingEqualsShipping, BillingEqualsShipping),
	BillingLastName = COALESCE(@BillingLastName, BillingLastName),
	BillingFirstName = COALESCE(@BillingFirstName, BillingFirstName),
	BillingCompany = COALESCE(@BillingCompany, BillingCompany),
	BillingAddress1 = COALESCE(@BillingAddress1, BillingAddress1),
	BillingAddress2 = COALESCE(@BillingAddress2, BillingAddress2),
	BillingSuite = COALESCE(@BillingSuite, BillingSuite),
	BillingCity = COALESCE(@BillingCity, BillingCity),
	BillingState = COALESCE(@BillingState, BillingState),
	BillingZip = COALESCE(@BillingZip, BillingZip),
	BillingCountry = COALESCE(@BillingCountry, BillingCountry),
	BillingPhone = COALESCE(@BillingPhone, BillingPhone),
	ShippingLastName = COALESCE(@ShippingLastName, ShippingLastName),
	ShippingFirstName = COALESCE(@ShippingFirstName, ShippingFirstName),
	ShippingCompany = COALESCE(@ShippingCompany, ShippingCompany),
	ShippingResidenceType = COALESCE(@ShippingResidenceType, ShippingResidenceType),
	ShippingAddress1 = COALESCE(@ShippingAddress1, ShippingAddress1),
	ShippingAddress2 = COALESCE(@ShippingAddress2, ShippingAddress2),
	ShippingSuite = COALESCE(@ShippingSuite, ShippingSuite),
	ShippingCity = COALESCE(@ShippingCity, ShippingCity),
	ShippingState = COALESCE(@ShippingState, ShippingState),
	ShippingZip = COALESCE(@ShippingZip, ShippingZip),
	ShippingCountry = COALESCE(@ShippingCountry, ShippingCountry),
	ShippingMethodID = COALESCE(@ShippingMethodID, ShippingMethodID),
	ShippingMethod = COALESCE(@ShippingMethod, ShippingMethod),
	ShippingPhone = COALESCE(@ShippingPhone, ShippingPhone),
	ShippingCalculationID = COALESCE(@ShippingCalculationID, ShippingCalculationID),
	Phone = COALESCE(@Phone, Phone),
	RegisterDate = COALESCE(@RegisterDate, RegisterDate),
	AffiliateID = COALESCE(@AffiliateID, AffiliateID),
	CouponCode = COALESCE(@CouponCode, CouponCode),
	CouponType = COALESCE(@CouponType, CouponType),
	CouponDescription = COALESCE(@CouponDescription, CouponDescription),
	CouponDiscountAmount = COALESCE(@CouponDiscountAmount, CouponDiscountAmount),
	CouponDiscountPercent = COALESCE(@CouponDiscountPercent, CouponDiscountPercent),
	CouponIncludesFreeShipping = COALESCE(@CouponIncludesFreeShipping, CouponIncludesFreeShipping),
	OkToEmail = COALESCE(@OkToEmail, OkToEmail),
	Deleted = COALESCE(@Deleted, Deleted),
	CardType = COALESCE(@CardType, CardType),
	CardName = COALESCE(@CardName, CardName),
	CardNumber = COALESCE(@CardNumber, CardNumber),
	CardExpirationMonth = COALESCE(@CardExpirationMonth, CardExpirationMonth),
	CardExpirationYear = COALESCE(@CardExpirationYear, CardExpirationYear),
	OrderSubtotal = COALESCE(@OrderSubtotal, OrderSubtotal),
	OrderTax = COALESCE(@OrderTax, OrderTax),
	OrderShippingCosts = COALESCE(@OrderShippingCosts, OrderShippingCosts),
	OrderTotal = COALESCE(@OrderTotal, OrderTotal),
	PaymentGateway = COALESCE(@PaymentGateway, PaymentGateway),
	AuthorizationCode = COALESCE(@AuthorizationCode, AuthorizationCode),
	AuthorizationResult = COALESCE(@AuthorizationResult, AuthorizationResult),
	AuthorizationPNREF = COALESCE(@AuthorizationPNREF, AuthorizationPNREF),
	TransactionCommand = COALESCE(@TransactionCommand, TransactionCommand),
	OrderDate = COALESCE(@OrderDate, OrderDate),
	LevelID = COALESCE(@LevelID, LevelID),
	LevelName = COALESCE(@LevelName, LevelName),
	LevelDiscountPercent = COALESCE(@LevelDiscountPercent, LevelDiscountPercent),
	LevelDiscountAmount = COALESCE(@LevelDiscountAmount, LevelDiscountAmount),
	LevelHasFreeShipping = COALESCE(@LevelHasFreeShipping, LevelHasFreeShipping),
	LevelAllowsQuantityDiscounts = COALESCE(@LevelAllowsQuantityDiscounts, LevelAllowsQuantityDiscounts),
	LevelHasNoTax = COALESCE(@LevelHasNoTax, LevelHasNoTax),
	LevelAllowsCoupons = COALESCE(@LevelAllowsCoupons, LevelAllowsCoupons),
	LevelDiscountsApplyToExtendedPrices = COALESCE(@LevelDiscountsApplyToExtendedPrices, LevelDiscountsApplyToExtendedPrices),
	LastIPAddress = COALESCE(@LastIPAddress, LastIPAddress),
	PaymentMethod = COALESCE(@PaymentMethod, PaymentMethod),
	OrderNotes = COALESCE(@OrderNotes, OrderNotes),
	PONumber = COALESCE(@PONumber, PONumber),
	DownloadEmailSentOn = COALESCE(@DownloadEmailSentOn, DownloadEmailSentOn),
	ReceiptEmailSentOn = COALESCE(@ReceiptEmailSentOn, ReceiptEmailSentOn),
	DistributorEmailSentOn = COALESCE(@DistributorEmailSentOn, DistributorEmailSentOn),
	ShippingTrackingNumber = COALESCE(@ShippingTrackingNumber, ShippingTrackingNumber),
	ShippedVIA = COALESCE(@ShippedVIA, ShippedVIA),
	CustomerServiceNotes = COALESCE(@CustomerServiceNotes, CustomerServiceNotes),
	RTShipRequest = COALESCE(@RTShipRequest, RTShipRequest),
	RTShipResponse = COALESCE(@RTShipResponse, RTShipResponse),
	TransactionState = COALESCE(@TransactionState, TransactionState),
	AVSResult = COALESCE(@AVSResult, AVSResult),
	CaptureTXCommand = COALESCE(@CaptureTXCommand, CaptureTXCommand),
	CaptureTXResult = COALESCE(@CaptureTXResult, CaptureTXResult),
	VoidTXCommand = COALESCE(@VoidTXCommand, VoidTXCommand),
	VoidTXResult = COALESCE(@VoidTXResult, VoidTXResult),
	RefundTXCommand = COALESCE(@RefundTXCommand, RefundTXCommand),
	RefundTXResult = COALESCE(@RefundTXResult, RefundTXResult),
	CardinalLookupResult = COALESCE(@CardinalLookupResult, CardinalLookupResult),
	CardinalAuthenticateResult = COALESCE(@CardinalAuthenticateResult, CardinalAuthenticateResult),
	CardinalGatewayParms = COALESCE(@CardinalGatewayParms, CardinalGatewayParms),
	AffiliateCommissionRecorded = COALESCE(@AffiliateCommissionRecorded, AffiliateCommissionRecorded),
	OrderOptions = COALESCE(@OrderOptions, OrderOptions),
	OrderWeight = COALESCE(@OrderWeight, OrderWeight),
	CarrierReportedRate = COALESCE(@CarrierReportedRate, CarrierReportedRate),
	CarrierReportedWeight = COALESCE(@CarrierReportedWeight, CarrierReportedWeight),
	LocaleSetting = COALESCE(@LocaleSetting, LocaleSetting),
	FinalizationData = COALESCE(@FinalizationData, FinalizationData),
	ExtensionData = COALESCE(@ExtensionData, ExtensionData),
	AlreadyConfirmed = COALESCE(@AlreadyConfirmed, AlreadyConfirmed),
	CartType = COALESCE(@CartType, CartType),
	Last4 = COALESCE(@Last4, Last4),
	ReadyToShip = COALESCE(@ReadyToShip, ReadyToShip),
	IsPrinted = COALESCE(@IsPrinted, IsPrinted),
	AuthorizedOn = COALESCE(@AuthorizedOn, AuthorizedOn),
	CapturedOn = COALESCE(@CapturedOn, CapturedOn),
	RefundedOn = COALESCE(@RefundedOn, RefundedOn),
	VoidedOn = COALESCE(@VoidedOn, VoidedOn),
	InventoryWasReduced = COALESCE(@InventoryWasReduced, InventoryWasReduced),
	MaxMindFraudScore = COALESCE(@MaxMindFraudScore, MaxMindFraudScore),
	MaxMindDetails = COALESCE(@MaxMindDetails, MaxMindDetails),
	CardStartDate = COALESCE(@CardStartDate, CardStartDate),
	CardIssueNumber = COALESCE(@CardIssueNumber, CardIssueNumber),
	TransactionType = COALESCE(@TransactionType, TransactionType),
	Crypt = COALESCE(@Crypt, Crypt),
	VATRegistrationID = COALESCE(@VATRegistrationID, VATRegistrationID),
	FraudedOn = COALESCE(@FraudedOn, FraudedOn),
	RefundReason = COALESCE(@RefundReason, RefundReason)
where OrderNumber = @OrderNumber
GO

insert CustomerDataRetention (CustomerId, CustomerLastActiveOn)
select CustomerId, UpdatedOn from Customer
where CustomerId not in 
	(select CustomerId from CustomerDataRetention)

/*********** End 10.0.15 Changes *******************/

PRINT CHAR(10)
PRINT '*****Finalizing database upgrade*****'
-- Update database indexes
PRINT 'Updating database indexes...'
EXEC [dbo].[aspdnsf_UpdateIndexes]
-- Update store version
PRINT 'Updating Store Version...'
UPDATE [dbo].[AppConfig] SET [ConfigValue] = '10.0.15' WHERE [Name] = 'StoreVersion'
print '*****Database Upgrade Completed*****'

SET NOEXEC OFF
GO

