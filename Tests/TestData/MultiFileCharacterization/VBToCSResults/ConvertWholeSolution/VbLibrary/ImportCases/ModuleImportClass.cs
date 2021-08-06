using static VbNetStandardLib.ModuleWithClassAndMethod;

namespace VbLibrary.ModuleImport
{
    class ModuleImportClass
    {
        public void ModuleImportUnQualifiedMember()
        {
            ModuleMethod();
        }

        public void ModuleImportUnQualifiedNestedType()
        {
            var mc = new ModuleClass();
            mc.ModuleClassMethod();
        }
    }
}