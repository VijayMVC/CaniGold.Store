-- ------------------------------------------------------------------------------------------
-- Copyright AspDotNetStorefront.com.  All Rights Reserved.
-- http://www.aspdotnetstorefront.com
-- For details on this license please visit our homepage at the URL above.
-- THE ABOVE NOTICE MUST REMAIN INTACT.
-- ------------------------------------------------------------------------------------------
-- ---------------------------------------------------------------------------------------------------------
-- This optional script will convert your old skin path from version 9 to your new version 10 skin directory
-- Make sure to set your old skin id properly below
-- You may need to run this for each skin you have
-- ---------------------------------------------------------------------------------------------------------

declare @OldSkinId varchar(20) = ''

IF @OldSkinId = ''
BEGIN
	print 'THE SCRIPT DID NOT REALLY RUN.'
	print 'MAKE SURE TO SET YOUR @OldSkinId variable.'
END
ELSE
BEGIN
	print 'Updating Topics'
	update Topic set [description] = replace([description], '/App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_' + @OldSkinId + '/%'
	update Topic set [description] = replace([description], '/App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_' + @OldSkinId + '/%'
	update Topic set [description] = replace([description], '/App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_(!SKINID!)/%'
	update Topic set [description] = replace([description], '/App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_(!SKINID!)/%'
	update Topic set [description] = replace([description], 'App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_' + @OldSkinId + '/%'
	update Topic set [description] = replace([description], 'App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_' + @OldSkinId + '/%'
	update Topic set [description] = replace([description], 'App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_(!SKINID!)/%'
	update Topic set [description] = replace([description], 'App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_(!SKINID!)/%'
	
	print 'Updating Products'
	update Product set [description] = replace([description], '/App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_' + @OldSkinId + '/%'
	update Product set [description] = replace([description], '/App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_' + @OldSkinId + '/%'
	update Product set [description] = replace([description], '/App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_(!SKINID!)/%'
	update Product set [description] = replace([description], '/App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_(!SKINID!)/%'
	update Product set [description] = replace([description], 'App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_' + @OldSkinId + '/%'
	update Product set [description] = replace([description], 'App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_' + @OldSkinId + '/%'
	update Product set [description] = replace([description], 'App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_(!SKINID!)/%'
	update Product set [description] = replace([description], 'App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_(!SKINID!)/%'

	update Product set Summary = replace(Summary, '/App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_' + @OldSkinId + '/%'
	update Product set Summary = replace(Summary, '/App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_' + @OldSkinId + '/%'
	update Product set Summary = replace(Summary, '/App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_(!SKINID!)/%'
	update Product set Summary = replace(Summary, '/App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_(!SKINID!)/%'
	update Product set Summary = replace(Summary, 'App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_' + @OldSkinId + '/%'
	update Product set Summary = replace(Summary, 'App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_' + @OldSkinId + '/%'
	update Product set Summary = replace(Summary, 'App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_(!SKINID!)/%'
	update Product set Summary = replace(Summary, 'App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_(!SKINID!)/%'

	print 'Updating Categories'
	update Category set [description] = replace([description], '/App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_' + @OldSkinId + '/%'
	update Category set [description] = replace([description], '/App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_' + @OldSkinId + '/%'
	update Category set [description] = replace([description], '/App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_(!SKINID!)/%'
	update Category set [description] = replace([description], '/App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_(!SKINID!)/%'
	update Category set [description] = replace([description], 'App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_' + @OldSkinId + '/%'
	update Category set [description] = replace([description], 'App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_' + @OldSkinId + '/%'
	update Category set [description] = replace([description], 'App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_(!SKINID!)/%'
	update Category set [description] = replace([description], 'App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_(!SKINID!)/%'

	update Category set Summary = replace(Summary, '/App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_' + @OldSkinId + '/%'
	update Category set Summary = replace(Summary, '/App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_' + @OldSkinId + '/%'
	update Category set Summary = replace(Summary, '/App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_(!SKINID!)/%'
	update Category set Summary = replace(Summary, '/App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_(!SKINID!)/%'
	update Category set Summary = replace(Summary, 'App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_' + @OldSkinId + '/%'
	update Category set Summary = replace(Summary, 'App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_' + @OldSkinId + '/%'
	update Category set Summary = replace(Summary, 'App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_(!SKINID!)/%'
	update Category set Summary = replace(Summary, 'App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_(!SKINID!)/%'

	print 'Updating Manufacturers'
	update Manufacturer set [description] = replace([description], '/App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_' + @OldSkinId + '/%'
	update Manufacturer set [description] = replace([description], '/App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_' + @OldSkinId + '/%'
	update Manufacturer set [description] = replace([description], '/App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_(!SKINID!)/%'
	update Manufacturer set [description] = replace([description], '/App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_(!SKINID!)/%'
	update Manufacturer set [description] = replace([description], 'App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_' + @OldSkinId + '/%'
	update Manufacturer set [description] = replace([description], 'App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_' + @OldSkinId + '/%'
	update Manufacturer set [description] = replace([description], 'App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_(!SKINID!)/%'
	update Manufacturer set [description] = replace([description], 'App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_(!SKINID!)/%'

	update Manufacturer set Summary = replace(Summary, '/App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_' + @OldSkinId + '/%'
	update Manufacturer set Summary = replace(Summary, '/App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_' + @OldSkinId + '/%'
	update Manufacturer set Summary = replace(Summary, '/App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_(!SKINID!)/%'
	update Manufacturer set Summary = replace(Summary, '/App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_(!SKINID!)/%'
	update Manufacturer set Summary = replace(Summary, 'App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_' + @OldSkinId + '/%'
	update Manufacturer set Summary = replace(Summary, 'App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_' + @OldSkinId + '/%'
	update Manufacturer set Summary = replace(Summary, 'App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_(!SKINID!)/%'
	update Manufacturer set Summary = replace(Summary, 'App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_(!SKINID!)/%'

	print 'Updating Sections'
	update Section set [description] = replace([description], '/App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_' + @OldSkinId + '/%'
	update Section set [description] = replace([description], '/App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_' + @OldSkinId + '/%'
	update Section set [description] = replace([description], '/App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_(!SKINID!)/%'
	update Section set [description] = replace([description], '/App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_(!SKINID!)/%'
	update Section set [description] = replace([description], 'App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_' + @OldSkinId + '/%'
	update Section set [description] = replace([description], 'App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_' + @OldSkinId + '/%'
	update Section set [description] = replace([description], 'App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_(!SKINID!)/%'
	update Section set [description] = replace([description], 'App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_(!SKINID!)/%'

	update Section set Summary = replace(Summary, '/App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_' + @OldSkinId + '/%'
	update Section set Summary = replace(Summary, '/App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_' + @OldSkinId + '/%'
	update Section set Summary = replace(Summary, '/App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Templates/Skin_(!SKINID!)/%'
	update Section set Summary = replace(Summary, '/App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%/App_Themes/Skin_(!SKINID!)/%'
	update Section set Summary = replace(Summary, 'App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_' + @OldSkinId + '/%'
	update Section set Summary = replace(Summary, 'App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_' + @OldSkinId + '/%'
	update Section set Summary = replace(Summary, 'App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Templates/Skin_(!SKINID!)/%'
	update Section set Summary = replace(Summary, 'App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where [description] like '%App_Themes/Skin_(!SKINID!)/%'

	print 'Updating News'
	update News set NewsCopy = replace(NewsCopy, '/App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where NewsCopy like '%/App_Templates/Skin_' + @OldSkinId + '/%'
	update News set NewsCopy = replace(NewsCopy, '/App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where NewsCopy like '%/App_Themes/Skin_' + @OldSkinId + '/%'
	update News set NewsCopy = replace(NewsCopy, '/App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where NewsCopy like '%/App_Templates/Skin_(!SKINID!)/%'
	update News set NewsCopy = replace(NewsCopy, '/App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where NewsCopy like '%/App_Themes/Skin_(!SKINID!)/%'
	update News set NewsCopy = replace(NewsCopy, 'App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where NewsCopy like '%App_Templates/Skin_' + @OldSkinId + '/%'
	update News set NewsCopy = replace(NewsCopy, 'App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where NewsCopy like '%App_Themes/Skin_' + @OldSkinId + '/%'
	update News set NewsCopy = replace(NewsCopy, 'App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where NewsCopy like '%App_Templates/Skin_(!SKINID!)/%'
	update News set NewsCopy = replace(NewsCopy, 'App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where NewsCopy like '%App_Themes/Skin_(!SKINID!)/%'

	update News set Headline = replace(Headline, '/App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where Headline like '%/App_Templates/Skin_' + @OldSkinId + '/%'
	update News set Headline = replace(Headline, '/App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where Headline like '%/App_Themes/Skin_' + @OldSkinId + '/%'
	update News set Headline = replace(Headline, '/App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where Headline like '%/App_Templates/Skin_(!SKINID!)/%'
	update News set Headline = replace(Headline, '/App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where Headline like '%/App_Themes/Skin_(!SKINID!)/%'
	update News set Headline = replace(Headline, 'App_Templates/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where Headline like '%App_Templates/Skin_' + @OldSkinId + '/%'
	update News set Headline = replace(Headline, 'App_Themes/Skin_' + @OldSkinId + '/', '(!SkinPath!)/')
	where Headline like '%App_Themes/Skin_' + @OldSkinId + '/%'
	update News set Headline = replace(Headline, 'App_Templates/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where Headline like '%App_Templates/Skin_(!SKINID!)/%'
	update News set Headline = replace(Headline, 'App_Themes/Skin_(!SKINID!)/', '(!SkinPath!)/')
	where Headline like '%App_Themes/Skin_(!SKINID!)/%'

END
GO
