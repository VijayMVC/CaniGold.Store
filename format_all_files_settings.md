# Install *Format All Files* Extension

- Visual Studio>Tools and Extensions > Search for "Format All Files"
- Or go here: https://marketplace.visualstudio.com/items?itemName=munyabe.FormatAllFiles
- We used this extension to reformat all files in the solution, with some exceptions. (see below)
- Running the following two extensions on your project should, along with the new *.editorconfig* file, allow a more successful merge with custom code.
- We've had good success resolving differences with Beyond Compare software.

## Settings (Format All Files)

- Enable Format Document: True
- Enable Remove and Sort Usings: True
- Exclude Generated T4: True

- Inclusion Pattern:
```
*.asax;*.ascx;*.aspx;*.config;*.cs;*.cshtml;*.css;*.htm;*.html;*.js;*.json;*.layout;theme.less;*.master;*.xml;
```
- Exclusion Pattern:
```
buysafeframe.aspx;jquery.js;jquery.*.js;*.min.css;*.min.js;bootstrap.js;font-awesome.css;fonts.css;bootstrap.css;*.designer.cs;Settings.Designer.cs;Reference.cs;card.js
```

### Use *Format On Save* Extension

- Every time you save the file it applies the .editorconfig settings, Remove And Sort usings, etc.
- https://marketplace.visualstudio.com/items?itemName=WinstonFeng.FormatonSave
- **Note**: behavior with *cshtml* files can be inconsistent when running on a folder such as *Views* vs. opening each file and saving.
- There are also some files on the exclusion list that Ctrl K+D does not work effectively on. Mostly where an `if` statement is multi-line, and if @using is used.

#### *Format on Save* Settings

- AllowExtensions: 
```
.asax .ascx .aspx .config .cs .cshtml .css .htm .html .js .json .layout theme.less .master .xml
```
- AllowForceUtf8WithBomExtensions: `blank`
- AllowFormatDocumentExtensions: `blank`
- DenyExtensions:
```
buysafeframe.aspx jquery.js jquery. .js .min.css .min.js bootstrap.js font-awesome.css fonts.css bootstrap.css .designer.cs Settings.Designer.cs Reference.cs
```
- DenyForceUtf8WithBomExtensions: `blank`
- DenyFormatDocumentExtensions: `blank`
- DenyFormatDocumentExtensions: `_AddressOptions.cshtml _KitAddToCartForm.cshtml KitGroup.cshtml KitItemFileUpload.cshtml KitItemMultiSelectCheckbox.cshtml KitItemTextArea.cshtml KitItemTextOption.cshtml`
- EnableForceUtf8WithBom: `false` Don't see any reason to do this. https://stackoverflow.com/questions/2223882/whats-different-between-utf-8-and-utf-8-without-bom
- EnableFormatDocument: `true` (This is ctrl K+D)
- EnableRemoveAndSort: `true` (Same as Edit > IntelliSense > Remove and Sort Usings)
- EnableRemoveTrailingSpaces: `true` (Normalize Whitespace, a.k.a. remove spaces on end of lines.)
- EnableSmartRemoveAndSort: `true` (Only apply remove and sort to C# files without #if directives)
- EnableTabToSpace: `true` (Counter-intuitive, since if you're set to use Tabs, it converts appropriate spaces to Tabs. Depends on VS settings)
- EnableUnifyEndOfFile: `true` (Enforce only one line-break at end of file)
- EnableUnifyLineBreak: `true` (Enforce consistent line-break settings within a file.)
- LineBreak: `Windows` (crlf)
