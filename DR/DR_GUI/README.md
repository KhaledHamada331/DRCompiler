# DR Language Compiler - GUI Application

## كيفية التشغيل (How to Run)

### الطريقة الأولى: من Visual Studio
1. افتح ملف `DR.sln` في Visual Studio
2. اضغط بزر الماوس الأيمن على مشروع `DR_GUI` في Solution Explorer
3. اختر **"Set as StartUp Project"**
4. اضغط **F5** أو اضغط على زر **Start** الأخضر

### الطريقة الثانية: تشغيل مباشر
1. افتح Command Prompt أو PowerShell
2. اذهب إلى مجلد المشروع:
   ```
   cd "DR\DR_GUI"
   ```
3. قم بتشغيل:
   ```
   msbuild DR_GUI.csproj /p:Configuration=Debug
   ```
4. ثم شغل الملف:
   ```
   bin\Debug\DR_GUI.exe
   ```

## المميزات

✅ واجهة رسومية حديثة وجميلة  
✅ محرر كود مع مثال جاهز  
✅ فتح وحفظ الملفات (.dr)  
✅ عرض التوكنز (Tokens)  
✅ عرض جدول الرموز (Symbol Table)  
✅ عرض الأخطاء (Errors)  
✅ عرض شجرة التحليل (Parse Tree)  

## استخدام البرنامج

1. **New**: لإنشاء ملف جديد
2. **Open**: لفتح ملف `.dr`
3. **Save**: لحفظ الملف
4. **▶ Run Compiler**: لتشغيل المترجم على الكود

---

Made with ❤️ for DR Language Compiler

