using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

namespace IFCParser
{
    /// <summary>
    /// 描述一个ifc对象
    /// </summary>
    class IFCItem
    {
        #region 私有属性
        private Dictionary<string, string> Attributes;
        #endregion

        public IFCItem(Dictionary<string, string> attributes)
        {
            // TODO: Complete member initialization
            this.Attributes = attributes;
        }

        #region 获取属性值的方法

        public Dictionary<string, string> getAttributes()
        {
            return this.Attributes;
        }


        public void addAttributes(Dictionary<string, string> newAttributes) {
            foreach (string key in newAttributes.Keys) {
                if (this.Attributes.ContainsKey(key)) {
                    continue;
                }
                this.Attributes.Add(key, newAttributes[key]);
            }
        }
        #endregion
    }

    /// <summary>
    /// 描述IFC结构树中的一个item
    /// </summary>
    class IFCTreeItem
    {
        /// <summary>
        /// Instance.
        /// </summary>
        public int instance = -1;

        /// <summary>
        /// Node.
        /// </summary>
        public TreeNode treeNode = null;
    }

    class IFCParser
    {
        //私有属性，用来存储ifc结构树
        private TreeView ifcTree;
        private int ifcModel;
        private Dictionary<string, IFCItem> ifcDict;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ifcTree"></param>
        public IFCParser(TreeView ifcTree) {
            this.ifcTree = ifcTree;
            this.ifcDict = new Dictionary<string, IFCItem>();
        }

        /// <summary>
        /// 重置存放ifc信息的字典对象
        /// </summary>
        public void resetDict() {
            ifcDict = new Dictionary<string, IFCItem>();
        }

        /// <summary>
        /// 获取存放ifc信息的字典对象        
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, IFCItem> getIFCDict() {
            return this.ifcDict;
        }

        #region 解析IFC文件
        /// <summary>
        /// 解析IFC文件
        /// </summary>
        /// <param name="ifcFilePath"></param>
        /// <returns></returns>
        public bool parseIfcFile(string ifcFilePath) {
            //如果文件存在
            if(File.Exists(ifcFilePath) == true){
                //尝试用IFC2X3打开文件
                this.ifcModel = IfcEngine.x86.sdaiOpenModelBN(0, ifcFilePath, "IFC2X3_TC1.exp");

                string xmlSettings_IFC2x3 = @"IFC2X3-Settings.xml";
                string xmlSettings_IFC4 = @"IFC4-Settings.xml";

                //不为0，说明文件打开成功
                if (ifcModel != 0) {
                    IntPtr outputValue = IntPtr.Zero;
                    //读取IFC文件中头信息中的FILE_SCHEMA信息
                    IfcEngine.x86.GetSPFFHeaderItem(ifcModel, 9, 0, IfcEngine.x86.sdaiUNICODE, out outputValue);
                    string fileSchema = Marshal.PtrToStringUni(outputValue);

                    XmlTextReader textReader = null;
                    if (fileSchema.Contains("IFC2") == true)
                    {
                        textReader = new XmlTextReader(xmlSettings_IFC2x3);
                    }
                    else
                    {
                        if (fileSchema.Contains("IFC4") == true)
                        {
                            //使用了错误方式打开IFC文件，先关闭再重新打开
                            IfcEngine.x86.sdaiCloseModel(ifcModel);
                            ifcModel = IfcEngine.x86.sdaiOpenModelBN(0, ifcFilePath, "IFC4.exp");

                            if (ifcModel != 0)
                                textReader = new XmlTextReader(xmlSettings_IFC4);
                        }
                    }

                    if (textReader == null)
                        return false;

                    while (textReader.Read()) {
                        //移动到下一个节点
                        textReader.MoveToElement();
                        //如果节点名是object
                        if (textReader.LocalName == "object")
                        {
                            //如果节点名的name属性不为空
                            if (textReader.GetAttribute("name") != null)
                            {
                                string Name = textReader.GetAttribute("name").ToString();
                            }
                        }
                    }
                }
        
                //创建树
                buildTree();
                //关闭IFC文件
                IfcEngine.x86.sdaiCloseModel(ifcModel);
            }
            return true;
        }
        #endregion

        /// <summary>
        /// 私有方法，用来辅助创建树
        /// </summary>
        private void buildTree() {
            //清空树的节点
            ifcTree.Nodes.Clear();
            if (ifcModel < 0) { 
                throw new ArgumentException("Invalid model");
            }
            //创建头信息树节点
            createHeaderTreeItems();
            //创建项目树节点
            createProjectTreeItems();
            //创建非关联对象树节点
            createNotReferencedTreeItems();
        }
        
        /// <summary>
        /// 创建项目头信息树节点
        /// </summary>
        private void createHeaderTreeItems() {
            //存放头信息属性的字典
            Dictionary<string, string> headInfos = new Dictionary<string, string>();
            string attributeName = "";
            string attribute = "";

            //一级节点：头信息
            TreeNode tnHeaderInfo = ifcTree.Nodes.Add("头信息");
            //二级节点：描述
            TreeNode tnDescriptions = tnHeaderInfo.Nodes.Add("Descriptions");
            //Descriptions属性
            attributeName = "Descriptions";

            //获取description信息，并且创建三级节点
            int i = 0;
            IntPtr description;
            while (IfcEngine.x86.GetSPFFHeaderItem(ifcModel, 0, i++, IfcEngine.x86.sdaiUNICODE, out description) == 0) {
                TreeNode tnDescription = tnDescriptions.Nodes.Add("Description = " + Marshal.PtrToStringUni(description));
                attribute += Marshal.PtrToStringUni(description) + "\n";
            }
            headInfos.Add(attributeName, attribute);

            //获取implementationLevel并创建二级节点
            IntPtr implemetationLevel;
            IfcEngine.x86.GetSPFFHeaderItem(ifcModel, 1, 0, IfcEngine.x86.sdaiUNICODE, out implemetationLevel);
            TreeNode tnImplementationLevel = tnHeaderInfo.Nodes.Add("ImplemetationLevel = " + Marshal.PtrToStringUni(implemetationLevel));
            attributeName = "implementationLevel";
            attribute = Marshal.PtrToStringUni(implemetationLevel);
            headInfos.Add(attributeName, attribute);

            //获取Name并创建二级节点
            IntPtr name;
            IfcEngine.x86.GetSPFFHeaderItem(ifcModel, 2, 0, IfcEngine.x86.sdaiUNICODE, out name);
            TreeNode tnName = tnHeaderInfo.Nodes.Add("Name = " + Marshal.PtrToStringUni(name));
            attributeName = "Name";
            attribute = Marshal.PtrToStringUni(name);
            headInfos.Add(attributeName, attribute);

            //获取timeSpan并创建二级节点
            IntPtr timeSpan;
            IfcEngine.x86.GetSPFFHeaderItem(ifcModel, 3, 0, IfcEngine.x86.sdaiUNICODE, out timeSpan);
            TreeNode tnTimeSpan = tnHeaderInfo.Nodes.Add("TimeSpan = " + Marshal.PtrToStringUni(timeSpan));
            attributeName = "timeSpan";
            attribute = Marshal.PtrToStringUni(timeSpan);
            headInfos.Add(attributeName, attribute);

            //获取author并创建二级节点
            TreeNode tnAuthors = tnHeaderInfo.Nodes.Add("Author");
            i = 0;
            IntPtr author;
            attribute = "";
            while (IfcEngine.x86.GetSPFFHeaderItem(ifcModel, 4, i++, IfcEngine.x86.sdaiUNICODE, out author) == 0)
            {
                TreeNode tnAuthor = tnAuthors.Nodes.Add(Marshal.PtrToStringUni(author));
                attribute += Marshal.PtrToStringUni(author) + "\n";
            }
            attributeName = "author";
            headInfos.Add(attributeName, attribute);

            // 获取Organizations并创建二级节点
            TreeNode tnOrganizations = tnHeaderInfo.Nodes.Add("Organizations");
            i = 0;
            IntPtr organization;
            attribute = "";
            while (IfcEngine.x86.GetSPFFHeaderItem(ifcModel, 5, i++, IfcEngine.x86.sdaiUNICODE, out organization) == 0)
            {
                TreeNode tnOrganization = tnOrganizations.Nodes.Add(Marshal.PtrToStringUni(organization));
                attribute += Marshal.PtrToStringUni(organization) + "\n";
            }
            attributeName = "Organizations";
            headInfos.Add(attributeName, attribute);

            // 获取PreprocessorVersion并创建二级节点
            IntPtr preprocessorVersion;
            IfcEngine.x86.GetSPFFHeaderItem(ifcModel, 6, 0, IfcEngine.x86.sdaiUNICODE, out preprocessorVersion);
            TreeNode tnPreprocessorVersion = tnHeaderInfo.Nodes.Add("PreprocessorVersion = " + Marshal.PtrToStringUni(preprocessorVersion));
            attributeName = "PreprocessorVersion";
            attribute = Marshal.PtrToStringUni(preprocessorVersion);
            headInfos.Add(attributeName, attribute);

            // 获取OriginatingSystem并创建二级节点
            IntPtr originatingSystem;
            IfcEngine.x86.GetSPFFHeaderItem(ifcModel, 7, 0, IfcEngine.x86.sdaiUNICODE, out originatingSystem);
            TreeNode tnOriginatingSystem = tnHeaderInfo.Nodes.Add("OriginatingSystem = " + Marshal.PtrToStringUni(originatingSystem));
            attributeName = "OriginatingSystem";
            attribute = Marshal.PtrToStringUni(originatingSystem);
            headInfos.Add(attributeName, attribute);

            // 获取Authorization并创建二级节点
            IntPtr authorization;
            IfcEngine.x86.GetSPFFHeaderItem(ifcModel, 8, 0, IfcEngine.x86.sdaiUNICODE, out authorization);
            TreeNode tnAuthorization = tnHeaderInfo.Nodes.Add("Authorization = " + Marshal.PtrToStringUni(authorization));
            attributeName = "Authorization";
            attribute = Marshal.PtrToStringUni(authorization);
            headInfos.Add(attributeName, attribute);

            // 获取FileSchemas并创建二级节点
            TreeNode tnFileSchemas = tnHeaderInfo.Nodes.Add("FileSchemas");
            i = 0;
            IntPtr fileSchema;
            attribute = "";
            while (IfcEngine.x86.GetSPFFHeaderItem(ifcModel, 9, i++, IfcEngine.x86.sdaiUNICODE, out fileSchema) == 0)
            {
                TreeNode tnFileSchema = tnFileSchemas.Nodes.Add(Marshal.PtrToStringUni(fileSchema));
                attribute += Marshal.PtrToStringUni(fileSchema) + "\n";
    
            }
            attributeName = "FileSchemas";
            headInfos.Add(attributeName, attribute);

            //将header的信息存放到字典中
            ifcDict.Add("Header", new IFCItem(headInfos));
            //MessageBox.Show(IFCItemToString(ifcDict["Header"]), "Header");
        }
        
        private void createProjectTreeItems() {
            //获取IfcProject
            int iEntityID = IfcEngine.x86.sdaiGetEntityExtentBN(ifcModel, "IfcProject");
            //IfcProject实例的数目
            int iEntitiesCount = IfcEngine.x86.sdaiGetMemberCount(iEntityID);

            for (int iEntity = 0; iEntity < iEntitiesCount; iEntity++)
            {
                int iInstance = 0;
                IfcEngine.x86.engiGetAggrElement(iEntityID, iEntity, IfcEngine.x86.sdaiINSTANCE, out iInstance);

                IFCTreeItem ifcTreeItem = new IFCTreeItem();
                ifcTreeItem.instance = iInstance;

                CreateTreeItem(null, ifcTreeItem);
                AddChildrenTreeItems(ifcTreeItem, iInstance, "IfcSite");
            }
        }

        private void createNotReferencedTreeItems()
        { 
            
        }

        /// <summary>
        /// 创建树节点的方法
        /// </summary>
        /// <param name="instance"></param>
        private void CreateTreeItem(IFCTreeItem ifcParent, IFCTreeItem ifcItem) {
            //存放头信息属性的字典
            Dictionary<string, string> ifcProjectInfos = new Dictionary<string, string>();
            string attributeName = "";
            string attribute = "";

            //获取实例的类型（编号）
            int entity = IfcEngine.x86.sdaiGetInstanceType(ifcItem.instance);
            //获取实例的类型名称（IFCColumn之类）
            IntPtr entityNamePtr = IntPtr.Zero;
            IfcEngine.x86.engiGetEntityName(entity, IfcEngine.x86.sdaiUNICODE, out entityNamePtr);
            string strIfcType = Marshal.PtrToStringUni(entityNamePtr);
            attributeName = "IfcType";
            attribute = strIfcType;
            ifcProjectInfos.Add(attributeName, attribute);

            //获取IFCProject的各种属性
            IntPtr globalId;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "GlobalID", IfcEngine.x86.sdaiUNICODE, out globalId);
            string strGlobalId = Marshal.PtrToStringUni(globalId);
            attributeName = "GlobalID";
            attribute = strGlobalId;
            ifcProjectInfos.Add(attributeName, attribute);

            IntPtr name;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Name", IfcEngine.x86.sdaiUNICODE, out name);
            string strName = Marshal.PtrToStringUni(name);
            attributeName = "Name";
            attribute = strName;
            ifcProjectInfos.Add(attributeName, attribute);

            IntPtr description;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Description", IfcEngine.x86.sdaiUNICODE, out description);
            string strDescription = Marshal.PtrToStringUni(description);
            attributeName = "Description";
            attribute = strDescription;
            ifcProjectInfos.Add(attributeName, attribute);

            IntPtr objectType;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "ObjectType", IfcEngine.x86.sdaiUNICODE, out objectType);
            string strObjectType = Marshal.PtrToStringUni(objectType);
            attributeName = "ObjectType";
            attribute = strObjectType;
            ifcProjectInfos.Add(attributeName, attribute);

            IntPtr longName;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "LongName", IfcEngine.x86.sdaiUNICODE, out longName);
            string strLongName = Marshal.PtrToStringUni(longName);
            attributeName = "LongName";
            attribute = strLongName;
            ifcProjectInfos.Add(attributeName, attribute);

            IntPtr phase;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Phase", IfcEngine.x86.sdaiUNICODE, out phase);
            string strPhase = Marshal.PtrToStringUni(phase);
            attributeName = "Phase";
            attribute = strPhase;
            ifcProjectInfos.Add(attributeName, attribute);

            string strItemText = "\"" + strGlobalId + "\",\"" + (string.IsNullOrEmpty(strName) ? "<name>" : strName) +
                    "\",\"" + (string.IsNullOrEmpty(strDescription) ? "<description>" : strDescription) +
                    "\",\"" + (string.IsNullOrEmpty(strObjectType) ? "<objectType>" : strObjectType) +
                    "\",\"" + (string.IsNullOrEmpty(strLongName) ? "<longName>" : strLongName) +
                    "\",\"" + (string.IsNullOrEmpty(strPhase) ? "<phase>" : strPhase) +
                    "\", (" + strIfcType + ")";
            //创建树节点
            if ((ifcParent != null) && (ifcParent.treeNode != null))
            {
                ifcItem.treeNode = ifcParent.treeNode.Nodes.Add(strItemText);
            }
            else
            {
                ifcItem.treeNode =  ifcTree.Nodes.Add(strItemText);
            }

            if (IsInstanceOf(ifcItem.instance, "IFCELEMENTQUANTITY") || IsInstanceOf(ifcItem.instance, "IFCPROPERTYSET"))
            {
                string text = ifcParent.treeNode.Text;
                string parentItemId = text.Substring(1, text.IndexOf(',') - 2);
                ifcDict[parentItemId].addAttributes(ifcProjectInfos);
            }
            else {
                ifcDict.Add(strGlobalId, new IFCItem(ifcProjectInfos));
                
            }
            //MessageBox.Show(IFCItemToString(new IFCItem(ifcProjectInfos)));
        }


        private void AddChildrenTreeItems(IFCTreeItem ifcParent, int iParentInstance, string strEntityName)
        {
            // check for decomposition判断是否具有包含关系，如果没有包含关系表明已经是最顶层的实例了
            IntPtr decompositionInstance;
            IfcEngine.x86.sdaiGetAttrBN(iParentInstance, "IsDecomposedBy", IfcEngine.x86.sdaiAGGR, out decompositionInstance);

            if (decompositionInstance == IntPtr.Zero)//该实例已经不能被继续分解了
            {
                return;
            }

            int iDecompositionsCount = IfcEngine.x86.sdaiGetMemberCount(decompositionInstance.ToInt32());//得到组成实例的所有子实例的数目（组成ifcproject实例的其他实例的数目）
            for (int iDecomposition = 0; iDecomposition < iDecompositionsCount; iDecomposition++)
            {
                int iDecompositionInstance = 0;
                IfcEngine.x86.engiGetAggrElement(decompositionInstance.ToInt32(), iDecomposition, IfcEngine.x86.sdaiINSTANCE, out iDecompositionInstance);//得到与ifcproject有关联的其他实体的中间桥梁，如ifcrelaggregates、ifcreldefinesbyproperties等

                if (!IsInstanceOf(iDecompositionInstance, "IFCRELAGGREGATES"))//判断中间桥梁是不是IfcRelAggregates，如果是，则可以得到它的下一级实例
                {
                    continue;
                }

                IntPtr objectInstances;
                IfcEngine.x86.sdaiGetAttrBN(iDecompositionInstance, "RelatedObjects", IfcEngine.x86.sdaiAGGR, out objectInstances);//得到组成ifcproject实例的第i个子实例的集合

                int iObjectsCount = IfcEngine.x86.sdaiGetMemberCount(objectInstances.ToInt32());//得到组成实例的数目
                for (int iObject = 0; iObject < iObjectsCount; iObject++)
                {
                    int iObjectInstance = 0;
                    IfcEngine.x86.engiGetAggrElement(objectInstances.ToInt32(), iObject, IfcEngine.x86.sdaiINSTANCE, out iObjectInstance);

                    if (!IsInstanceOf(iObjectInstance, strEntityName))
                    {
                        continue;
                    }

                    IFCTreeItem ifcTreeItem = new IFCTreeItem();
                    ifcTreeItem.instance = iObjectInstance;

                    //程序原来的代码CreateTreeItem(ifcParent, ifcTreeItem);
                    //自己加的代码，用来根据子实例的不同获取不同的属性
                    switch (strEntityName)
                    {
                        case "IfcSite":
                            {
                                CreateIfcSiteTreeItem(ifcParent, ifcTreeItem);
                            }
                            break;
                        case "IfcBuilding":
                            {
                                CreateIfcBuildingTreeItem(ifcParent, ifcTreeItem);
                            }
                            break;
                        case "IfcBuildingStorey":
                            {
                                CreateIfcBuildingStoreyTreeItem(ifcParent, ifcTreeItem);
                            }
                            break;
                    }

                    switch (strEntityName)
                    {
                        case "IfcSite":
                            {
                                AddChildrenTreeItems(ifcTreeItem, iObjectInstance, "IfcBuilding");
                            }
                            break;

                        case "IfcBuilding":
                            {
                                AddChildrenTreeItems(ifcTreeItem, iObjectInstance, "IfcBuildingStorey");
                            }
                            break;

                        case "IfcBuildingStorey":
                            {
                                AddElementTreeItems(ifcTreeItem, iObjectInstance);
                            }
                            break;

                        default:
                            break;
                    }
                } // for (int iObject = ...
            } // for (int iDecomposition = ...
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="ifcParent"></param>
        /// <param name="iParentInstance"></param>     
        private void AddElementTreeItems(IFCTreeItem ifcParent, int iParentInstance)
        {
            IntPtr decompositionInstance;
            IfcEngine.x86.sdaiGetAttrBN(iParentInstance, "IsDecomposedBy", IfcEngine.x86.sdaiAGGR, out decompositionInstance);

            if (decompositionInstance == IntPtr.Zero)
            {
                return;
            }

            int iDecompositionsCount = IfcEngine.x86.sdaiGetMemberCount(decompositionInstance.ToInt32());
            for (int iDecomposition = 0; iDecomposition < iDecompositionsCount; iDecomposition++)
            {
                int iDecompositionInstance = 0;
                IfcEngine.x86.engiGetAggrElement(decompositionInstance.ToInt32(), iDecomposition, IfcEngine.x86.sdaiINSTANCE, out iDecompositionInstance);

                if (!IsInstanceOf(iDecompositionInstance, "IFCRELAGGREGATES"))
                {
                    continue;
                }

                IntPtr objectInstances;
                IfcEngine.x86.sdaiGetAttrBN(iDecompositionInstance, "RelatedObjects", IfcEngine.x86.sdaiAGGR, out objectInstances);

                int iObjectsCount = IfcEngine.x86.sdaiGetMemberCount(objectInstances.ToInt32());
                for (int iObject = 0; iObject < iObjectsCount; iObject++)
                {
                    int iObjectInstance = 0;
                    IfcEngine.x86.engiGetAggrElement(objectInstances.ToInt32(), iObject, IfcEngine.x86.sdaiINSTANCE, out iObjectInstance);

                    IFCTreeItem ifcTreeItem = new IFCTreeItem();
                    ifcTreeItem.instance = iObjectInstance;

                    //CreateTreeItem(ifcParent, ifcTreeItem);
                    CreateElementTreeItem(ifcParent, ifcTreeItem);


                    //试验看能不能得到ifcSpace的属性；perfect！！
                    if (GetItemType(iObjectInstance) == "IfcSpace")
                    {
                        IntPtr definedByInstances;
                        IfcEngine.x86.sdaiGetAttrBN(iObjectInstance, "IsDefinedBy", IfcEngine.x86.sdaiAGGR, out definedByInstances);

                        if (definedByInstances == IntPtr.Zero)
                        {
                            continue;
                        }

                        int iDefinedByCount = IfcEngine.x86.sdaiGetMemberCount(definedByInstances.ToInt32());
                        for (int iDefinedBy = 0; iDefinedBy < iDefinedByCount; iDefinedBy++)
                        {
                            int iDefinedByInstance = 0;
                            IfcEngine.x86.engiGetAggrElement(definedByInstances.ToInt32(), iDefinedBy, IfcEngine.x86.sdaiINSTANCE, out iDefinedByInstance);

                            if (IsInstanceOf(iDefinedByInstance, "IFCRELDEFINESBYPROPERTIES"))
                            {
                                AddPropertyTreeItems(ifcTreeItem, iDefinedByInstance);
                            }
                            else
                            {
                                if (IsInstanceOf(iDefinedByInstance, "IFCRELDEFINESBYTYPE"))
                                {
                                    // NA
                                }
                            }
                        }
                    }
                } // for (int iObject = ...
            } // for (int iDecomposition = ...

            // check for elements
            IntPtr elementsInstance;
            IfcEngine.x86.sdaiGetAttrBN(iParentInstance, "ContainsElements", IfcEngine.x86.sdaiAGGR, out elementsInstance);

            if (elementsInstance == IntPtr.Zero)
            {
                return;
            }

            int iElementsCount = IfcEngine.x86.sdaiGetMemberCount(elementsInstance.ToInt32());
            for (int iElement = 0; iElement < iElementsCount; iElement++)
            {
                int iElementInstance = 0;
                IfcEngine.x86.engiGetAggrElement(elementsInstance.ToInt32(), iElement, IfcEngine.x86.sdaiINSTANCE, out iElementInstance);

                if (!IsInstanceOf(iElementInstance, "IFCRELCONTAINEDINSPATIALSTRUCTURE"))
                {
                    continue;
                }

                IntPtr objectInstances;
                IfcEngine.x86.sdaiGetAttrBN(iElementInstance, "RelatedElements", IfcEngine.x86.sdaiAGGR, out objectInstances);

                int iObjectsCount = IfcEngine.x86.sdaiGetMemberCount(objectInstances.ToInt32());
                for (int iObject = 0; iObject < iObjectsCount; iObject++)
                {
                    int iObjectInstance = 0;
                    IfcEngine.x86.engiGetAggrElement(objectInstances.ToInt32(), iObject, IfcEngine.x86.sdaiINSTANCE, out iObjectInstance);

                    IFCTreeItem ifcTreeItem = new IFCTreeItem();
                    ifcTreeItem.instance = iObjectInstance;
                    
                    //CreateTreeItem(ifcParent, ifcTreeItem);
                    CreateElementTreeItem(ifcParent, ifcTreeItem);


                    IntPtr definedByInstances;
                    IfcEngine.x86.sdaiGetAttrBN(iObjectInstance, "IsDefinedBy", IfcEngine.x86.sdaiAGGR, out definedByInstances);

                    if (definedByInstances == IntPtr.Zero)
                    {
                        continue;
                    }

                    int iDefinedByCount = IfcEngine.x86.sdaiGetMemberCount(definedByInstances.ToInt32());
                    for (int iDefinedBy = 0; iDefinedBy < iDefinedByCount; iDefinedBy++)
                    {
                        int iDefinedByInstance = 0;
                        IfcEngine.x86.engiGetAggrElement(definedByInstances.ToInt32(), iDefinedBy, IfcEngine.x86.sdaiINSTANCE, out iDefinedByInstance);

                        if (IsInstanceOf(iDefinedByInstance, "IFCRELDEFINESBYPROPERTIES"))
                        {
                            AddPropertyTreeItems(ifcTreeItem, iDefinedByInstance);
                        }
                        else
                        {
                            if (IsInstanceOf(iDefinedByInstance, "IFCRELDEFINESBYTYPE"))
                            {
                                // NA
                            }
                        }
                    }
                } // for (int iObject = ...
            } // for (int iDecomposition = ...
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="ifcParent"></param>
        /// <param name="iParentInstance"></param>     
        private void AddPropertyTreeItems(IFCTreeItem ifcParent, int iParentInstance)
        {
            IntPtr propertyInstances;
            IfcEngine.x86.sdaiGetAttrBN(iParentInstance, "RelatingPropertyDefinition", IfcEngine.x86.sdaiINSTANCE, out propertyInstances);

            if (IsInstanceOf(propertyInstances.ToInt32(), "IFCELEMENTQUANTITY"))
            {
                IFCTreeItem ifcPropertySetTreeItem = new IFCTreeItem();
                ifcPropertySetTreeItem.instance = propertyInstances.ToInt32();

                CreateTreeItem(ifcParent, ifcPropertySetTreeItem);

                // check for quantity
                IntPtr quantitiesInstance;
                IfcEngine.x86.sdaiGetAttrBN(propertyInstances.ToInt32(), "Quantities", IfcEngine.x86.sdaiAGGR, out quantitiesInstance);

                if (quantitiesInstance == IntPtr.Zero)
                {
                    return;
                }

                int iQuantitiesCount = IfcEngine.x86.sdaiGetMemberCount(quantitiesInstance.ToInt32());
                for (int iQuantity = 0; iQuantity < iQuantitiesCount; iQuantity++)
                {
                    int iQuantityInstance = 0;
                    IfcEngine.x86.engiGetAggrElement(quantitiesInstance.ToInt32(), iQuantity, IfcEngine.x86.sdaiINSTANCE, out iQuantityInstance);

                    IFCTreeItem ifcQuantityTreeItem = new IFCTreeItem();
                    ifcQuantityTreeItem.instance = iQuantityInstance;

                    if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYLENGTH"))
                        CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYLENGTH");
                    else
                        if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYAREA"))
                            CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYAREA");
                        else
                            if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYVOLUME"))
                                CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYVOLUME");
                            else
                                if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYCOUNT"))
                                    CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYCOUNT");
                                else
                                    if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYWEIGTH"))
                                        CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYWEIGTH");
                                    else
                                        if (IsInstanceOf(iQuantityInstance, "IFCQUANTITYTIME"))
                                            CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcQuantityTreeItem, "IFCQUANTITYTIME");
                } // for (int iQuantity = ...
            }
            else
            {
                if (IsInstanceOf(propertyInstances.ToInt32(), "IFCPROPERTYSET"))
                {
                    IFCTreeItem ifcPropertySetTreeItem = new IFCTreeItem();
                    ifcPropertySetTreeItem.instance = propertyInstances.ToInt32();

                    CreateTreeItem(ifcParent, ifcPropertySetTreeItem);

                    // check for quantity
                    IntPtr propertiesInstance;
                    IfcEngine.x86.sdaiGetAttrBN(propertyInstances.ToInt32(), "HasProperties", IfcEngine.x86.sdaiAGGR, out propertiesInstance);

                    if (propertiesInstance == IntPtr.Zero)
                    {
                        return;
                    }

                    int iPropertiesCount = IfcEngine.x86.sdaiGetMemberCount(propertiesInstance.ToInt32());
                    for (int iProperty = 0; iProperty < iPropertiesCount; iProperty++)
                    {
                        int iPropertyInstance = 0;
                        IfcEngine.x86.engiGetAggrElement(propertiesInstance.ToInt32(), iProperty, IfcEngine.x86.sdaiINSTANCE, out iPropertyInstance);

                        if (!IsInstanceOf(iPropertyInstance, "IFCPROPERTYSINGLEVALUE"))
                            continue;

                        IFCTreeItem ifcPropertyTreeItem = new IFCTreeItem();
                        ifcPropertyTreeItem.instance = iPropertyInstance;

                        CreatePropertyTreeItem(ifcPropertySetTreeItem, ifcPropertyTreeItem, "IFCPROPERTYSINGLEVALUE");
                    } // for (int iProperty = ...
                }
            }
        }

        /// <summary>
        /// Helper
        /// </summary>
        /// <param name="ifcParent"></param>
        /// <param name="ifcItem"></param>
        private void CreatePropertyTreeItem(IFCTreeItem ifcParent, IFCTreeItem ifcItem, string strProperty)
        {
            Dictionary<string, string> propertyInfos = new Dictionary<string, string>();
            string attributeName = "";
            string attribute = "";

            //IntPtr ifcType = IfcEngine.x86.engiGetInstanceClassInfo(ifcItem.instance);
            int entity = IfcEngine.x86.sdaiGetInstanceType(ifcItem.instance);
            IntPtr entityNamePtr = IntPtr.Zero;
            IfcEngine.x86.engiGetEntityName(entity, IfcEngine.x86.sdaiUNICODE, out entityNamePtr);
            //string strIfcType = Marshal.PtrToStringAnsi(ifcType);
            string strIfcType = Marshal.PtrToStringUni(entityNamePtr);

            IntPtr name;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Name", IfcEngine.x86.sdaiUNICODE, out name);    

            string strName = Marshal.PtrToStringUni(name);
            attributeName = strName;

            string strValue = string.Empty;
            switch (strProperty)
            {
                case "IFCQUANTITYLENGTH":
                    {
                        IntPtr value;
                        IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "LengthValue", IfcEngine.x86.sdaiUNICODE, out value);

                        strValue = Marshal.PtrToStringUni(value);
                    }
                    break;

                case "IFCQUANTITYAREA":
                    {
                        IntPtr value;
                        IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "AreaValue", IfcEngine.x86.sdaiUNICODE, out value);

                        strValue = Marshal.PtrToStringUni(value);
                    }
                    break;

                case "IFCQUANTITYVOLUME":
                    {
                        IntPtr value;
                        IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "VolumeValue", IfcEngine.x86.sdaiUNICODE, out value);

                        strValue = Marshal.PtrToStringUni(value);
                    }
                    break;

                case "IFCQUANTITYCOUNT":
                    {
                        IntPtr value;
                        IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "CountValue", IfcEngine.x86.sdaiUNICODE, out value);

                        strValue = Marshal.PtrToStringUni(value);
                    }
                    break;

                case "IFCQUANTITYWEIGTH":
                    {
                        IntPtr value;
                        IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "WeigthValue", IfcEngine.x86.sdaiUNICODE, out value);

                        strValue = Marshal.PtrToStringUni(value);
                    }
                    break;

                case "IFCQUANTITYTIME":
                    {
                        IntPtr value;
                        IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "TimeValue", IfcEngine.x86.sdaiUNICODE, out value);

                        strValue = Marshal.PtrToStringUni(value);
                    }
                    break;

                case "IFCPROPERTYSINGLEVALUE":
                    {
                        IntPtr value;
                        IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "NominalValue", IfcEngine.x86.sdaiUNICODE, out value);

                        strValue = Marshal.PtrToStringUni(value);
                    }
                    break;

                default:
                    throw new Exception("Unknown property.");
            } // switch (strProperty)    
            attribute = strValue;
            propertyInfos.Add(attributeName, attribute);

            string strItemText = "\"" + (string.IsNullOrEmpty(strName) ? "<name>" : strName) +
                    "\",\"" + (string.IsNullOrEmpty(strValue) ? "<value>" : strValue) +
                    "\",(" + strIfcType + ")";

            if ((ifcParent != null) && (ifcParent.treeNode != null))
            {
                ifcItem.treeNode = ifcParent.treeNode.Nodes.Add(strItemText);
            }
            else
            {
                ifcItem.treeNode = ifcTree.Nodes.Add(strItemText);
            }
            
            string text = ifcParent.treeNode.Parent.Text;
            ifcDict[text.Substring(1, text.IndexOf(',') -2)].addAttributes(propertyInfos);
        }



        /// <summary>
        /// 自定义的用来创建其他节点的方法
        /// </summary>
        /// <param name="ifcParent"></param>
        /// <param name="ifcItem"></param>
        private void CreateElementTreeItem(IFCTreeItem ifcParent, IFCTreeItem ifcItem)
        {
            //存放头信息属性的字典
            Dictionary<string, string> ifcElementInfos = new Dictionary<string, string>();
            string attributeName = "";
            string attribute = "";

            int entity = IfcEngine.x86.sdaiGetInstanceType(ifcItem.instance);
            IntPtr entityNamePtr = IntPtr.Zero;
            IfcEngine.x86.engiGetEntityName(entity, IfcEngine.x86.sdaiUNICODE, out entityNamePtr);
            string strIfcType = Marshal.PtrToStringUni(entityNamePtr);
            attributeName = "IfcType";
            attribute = strIfcType;
            ifcElementInfos.Add(attributeName, attribute);

            IntPtr globalId;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "GlobalID", IfcEngine.x86.sdaiUNICODE, out globalId);
            string strGlobalId = Marshal.PtrToStringUni(globalId);
            attributeName = "GlobalID";
            attribute = strGlobalId;
            ifcElementInfos.Add(attributeName, attribute);

            IntPtr name;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Name", IfcEngine.x86.sdaiUNICODE, out name);
            string strName = Marshal.PtrToStringUni(name);
            attributeName = "Name";
            attribute = strName;
            ifcElementInfos.Add(attributeName, attribute);

            IntPtr description;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Description", IfcEngine.x86.sdaiUNICODE, out description);
            string strDescription = Marshal.PtrToStringUni(description);
            attributeName = "Description";
            attribute = strDescription;
            ifcElementInfos.Add(attributeName, attribute);

            IntPtr objectType;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "ObjectType", IfcEngine.x86.sdaiUNICODE, out objectType);
            string strObjectType = Marshal.PtrToStringUni(objectType);
            attributeName = "ObjectType";
            attribute = strObjectType;
            ifcElementInfos.Add(attributeName, attribute);

            IntPtr longName;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "LongName", IfcEngine.x86.sdaiUNICODE, out longName);
            string strLongName = Marshal.PtrToStringUni(longName);
            attributeName = "LongName";
            attribute = strLongName;
            ifcElementInfos.Add(attributeName, attribute);

            IntPtr compositionType;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "CompositionType", IfcEngine.x86.sdaiUNICODE, out compositionType);
            string strCompositionType = Marshal.PtrToStringUni(compositionType);
            attributeName = "CompositionType";
            attribute = strCompositionType;
            ifcElementInfos.Add(attributeName, attribute);


            string strItemText = "\"" + strGlobalId + "\",\"" + (string.IsNullOrEmpty(strName) ? "<name>" : strName) +
                    "\",\"" + (string.IsNullOrEmpty(strDescription) ? "<description>" : strDescription) +
                    "\",\"" + (string.IsNullOrEmpty(strObjectType) ? "<objectType>" : strObjectType) +
                    "\",\"" + (string.IsNullOrEmpty(strLongName) ? "<longName>" : strLongName) +
                    "\",\"" + (string.IsNullOrEmpty(strCompositionType) ? "<compositionType>" : strCompositionType) +
                    "\",(" + strIfcType + ")";

            if ((ifcParent != null) && (ifcParent.treeNode != null))
            {
                ifcItem.treeNode = ifcParent.treeNode.Nodes.Add(strItemText);
            }
            else
            {
                ifcItem.treeNode = ifcTree.Nodes.Add(strItemText);
            }

            ifcDict.Add(strGlobalId, new IFCItem(ifcElementInfos));
            //MessageBox.Show(IFCItemToString(new IFCItem(ifcElementInfos)));
        }

        /// <summary>
        /// 用来创建IfcSite节点的方法
        /// </summary>
        /// <param name="ifcParent"></param>
        /// <param name="ifcItem"></param>
        private void CreateIfcSiteTreeItem(IFCTreeItem ifcParent, IFCTreeItem ifcItem)
        {
            //存放头信息属性的字典
            Dictionary<string, string> ifcSiteInfos = new Dictionary<string, string>();
            string attributeName = "";
            string attribute = "";

            int entity = IfcEngine.x86.sdaiGetInstanceType(ifcItem.instance);
            IntPtr entityNamePtr = IntPtr.Zero;
            IfcEngine.x86.engiGetEntityName(entity, IfcEngine.x86.sdaiUNICODE, out entityNamePtr);
            string strIfcType = Marshal.PtrToStringUni(entityNamePtr);
            attributeName = "IfcType";
            attribute = strIfcType;
            ifcSiteInfos.Add(attributeName, attribute);

            IntPtr globalId;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "GlobalID", IfcEngine.x86.sdaiUNICODE, out globalId);
            string strGlobalId = Marshal.PtrToStringUni(globalId);
            attributeName = "GlobalID";
            attribute = strGlobalId;
            ifcSiteInfos.Add(attributeName, attribute);

            //ownerHistory属性是另外一个实体，需要重新查找其属性 To do
            //int ownerHistory;
            //IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "OwnerHistory", IfcEngine.x86.sdaiINSTANCE, out ownerHistory);
            //int ownerHistoryEntity = IfcEngine.x86.sdaiGetInstanceType(ownerHistory);
            //entityNamePtr = IntPtr.Zero;
            //IfcEngine.x86.engiGetEntityName(ownerHistoryEntity, IfcEngine.x86.sdaiUNICODE, out entityNamePtr);
            //strIfcType = Marshal.PtrToStringUni(entityNamePtr);

            IntPtr name;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Name", IfcEngine.x86.sdaiUNICODE, out name);
            string strName = Marshal.PtrToStringUni(name);
            attributeName = "Name";
            attribute = strName;
            ifcSiteInfos.Add(attributeName, attribute);

            IntPtr description;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Description", IfcEngine.x86.sdaiUNICODE, out description);
            string strDescription = Marshal.PtrToStringUni(description);
            attributeName = "Description";
            attribute = strDescription;
            ifcSiteInfos.Add(attributeName, attribute);

            IntPtr objectType;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "ObjectType", IfcEngine.x86.sdaiUNICODE, out objectType);
            string strObjectType = Marshal.PtrToStringUni(objectType);
            attributeName = "ObjectType";
            attribute = strObjectType;
            ifcSiteInfos.Add(attributeName, attribute);

            IntPtr longName;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "LongName", IfcEngine.x86.sdaiUNICODE, out longName);
            string strLongName = Marshal.PtrToStringUni(longName);
            attributeName = "LongName";
            attribute = strLongName;
            ifcSiteInfos.Add(attributeName, attribute);

            IntPtr compositionType;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "CompositionType", IfcEngine.x86.sdaiUNICODE, out compositionType);
            string strCompositionType = Marshal.PtrToStringUni(compositionType);
            attributeName = "CompositionType";
            attribute = strCompositionType;
            ifcSiteInfos.Add(attributeName, attribute);

            //存在问题
            IntPtr refLatitude;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "RefLatitude", IfcEngine.x86.sdaiUNICODE, out refLatitude);
            string strRefLatitude = Marshal.PtrToStringUni(refLatitude);
            attributeName = "RefLatitude";
            attribute = strRefLatitude;
            ifcSiteInfos.Add(attributeName, attribute);

            //存在问题
            IntPtr refLongitude;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "RefLongitude", IfcEngine.x86.sdaiUNICODE, out refLongitude);
            string strRefLongitude = Marshal.PtrToStringUni(refLongitude);
            attributeName = "RefLongitude";
            attribute = strRefLongitude;
            ifcSiteInfos.Add(attributeName, attribute);

            IntPtr refElevation;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "RefElevation", IfcEngine.x86.sdaiUNICODE, out refElevation);
            string strRefElevation = Marshal.PtrToStringUni(refElevation);
            attributeName = "RefElevation";
            attribute = strRefElevation;
            ifcSiteInfos.Add(attributeName, attribute);

            IntPtr landTitleNumber;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "LandTitleNumber", IfcEngine.x86.sdaiUNICODE, out landTitleNumber);
            string strLandTitleNumber = Marshal.PtrToStringUni(landTitleNumber);
            attributeName = "LandTitleNumber";
            attribute = strLandTitleNumber;
            ifcSiteInfos.Add(attributeName, attribute);

            IntPtr siteAddress;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "SiteAddress", IfcEngine.x86.sdaiUNICODE, out siteAddress);
            string strSiteAddress = Marshal.PtrToStringUni(siteAddress);
            attributeName = "SiteAddress";
            attribute = strSiteAddress;
            ifcSiteInfos.Add(attributeName, attribute);

            string strItemText = "\"" + strGlobalId + "\",\"" + (string.IsNullOrEmpty(strName) ? "<name>" : strName) +
                    "\",\"" + (string.IsNullOrEmpty(strDescription) ? "<description>" : strDescription) +
                    "\",\"" + (string.IsNullOrEmpty(strObjectType) ? "<objectType>" : strObjectType) +
                    "\",\"" + (string.IsNullOrEmpty(strLongName) ? "<longName>" : strLongName) +
                    "\",\"" + (string.IsNullOrEmpty(strCompositionType) ? "<compositionType>" : strCompositionType) +
                    "\",\"" + (string.IsNullOrEmpty(strRefLatitude) ? "<refLatitude>" : strRefLatitude) +
                    "\",\"" + (string.IsNullOrEmpty(strRefLongitude) ? "<refLongitude>" : strRefLongitude) +
                    "\",\"" + (string.IsNullOrEmpty(strRefElevation) ? "<refElevation>" : strRefElevation) +
                    "\",\"" + (string.IsNullOrEmpty(strLandTitleNumber) ? "<landTitleNumber>" : strLandTitleNumber) +
                    "\",\"" + (string.IsNullOrEmpty(strSiteAddress) ? "<SiteAddress>" : strSiteAddress) +
                    "\",(" + strIfcType + ")";

            if ((ifcParent != null) && (ifcParent.treeNode != null))
            {
                ifcItem.treeNode = ifcParent.treeNode.Nodes.Add(strItemText);
            }
            else
            {
                ifcItem.treeNode = ifcTree.Nodes.Add(strItemText);
            }

            ifcDict.Add(strGlobalId, new IFCItem(ifcSiteInfos));
            //MessageBox.Show(IFCItemToString(new IFCItem(ifcSiteInfos)));
        }

        /// <summary>
        /// 用来创建IfcBuilding节点的方法
        /// </summary>
        /// <param name="ifcParent"></param>
        /// <param name="ifcItem"></param>
        private void CreateIfcBuildingTreeItem(IFCTreeItem ifcParent, IFCTreeItem ifcItem) {
            //存放头信息属性的字典
            Dictionary<string, string> ifcBuildingInfos = new Dictionary<string, string>();
            string attributeName = "";
            string attribute = "";

            int entity = IfcEngine.x86.sdaiGetInstanceType(ifcItem.instance);
            IntPtr entityNamePtr = IntPtr.Zero;
            IfcEngine.x86.engiGetEntityName(entity, IfcEngine.x86.sdaiUNICODE, out entityNamePtr);
            string strIfcType = Marshal.PtrToStringUni(entityNamePtr);
            attributeName = "IfcType";
            attribute = strIfcType;
            ifcBuildingInfos.Add(attributeName, attribute);

            IntPtr globalId;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "GlobalID", IfcEngine.x86.sdaiUNICODE, out globalId);
            string strGlobalId = Marshal.PtrToStringUni(globalId);
            attributeName = "GlobalID";
            attribute = strGlobalId;
            ifcBuildingInfos.Add(attributeName, attribute);

            IntPtr name;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Name", IfcEngine.x86.sdaiUNICODE, out name);
            string strName = Marshal.PtrToStringUni(name);
            attributeName = "Name";
            attribute = strName;
            ifcBuildingInfos.Add(attributeName, attribute);

            IntPtr description;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Description", IfcEngine.x86.sdaiUNICODE, out description);
            string strDescription = Marshal.PtrToStringUni(description);
            attributeName = "Description";
            attribute = strDescription;
            ifcBuildingInfos.Add(attributeName, attribute);

            IntPtr objectType;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "ObjectType", IfcEngine.x86.sdaiUNICODE, out objectType);
            string strObjectType = Marshal.PtrToStringUni(objectType);
            attributeName = "ObjectType";
            attribute = strObjectType;
            ifcBuildingInfos.Add(attributeName, attribute);

            IntPtr longName;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "LongName", IfcEngine.x86.sdaiUNICODE, out longName);
            string strLongName = Marshal.PtrToStringUni(longName);
            attributeName = "LongName";
            attribute = strLongName;
            ifcBuildingInfos.Add(attributeName, attribute);

            IntPtr compositionType;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "CompositionType", IfcEngine.x86.sdaiUNICODE, out compositionType);
            string strCompositionType = Marshal.PtrToStringUni(compositionType);
            attributeName = "CompositionType";
            attribute = strCompositionType;
            ifcBuildingInfos.Add(attributeName, attribute);

            //存在问题
            IntPtr elevationOfRefHeight;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "ElevationOfRefHeight", IfcEngine.x86.sdaiUNICODE, out elevationOfRefHeight);
            string strElevationOfRefHeight = Marshal.PtrToStringUni(elevationOfRefHeight);
            attributeName = "ElevationOfRefHeight";
            attribute = strElevationOfRefHeight;
            ifcBuildingInfos.Add(attributeName, attribute);

            //存在问题
            IntPtr elevationOfTerrain;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "ElevationOfTerrain", IfcEngine.x86.sdaiUNICODE, out elevationOfTerrain);
            string strElevationOfTerrain = Marshal.PtrToStringUni(elevationOfTerrain);
            attributeName = "ElevationOfTerrain";
            attribute = strElevationOfTerrain;
            ifcBuildingInfos.Add(attributeName, attribute);


            string strItemText = "\"" + strGlobalId + "\",\"" + (string.IsNullOrEmpty(strName) ? "<name>" : strName) +
                    "\",\"" + (string.IsNullOrEmpty(strDescription) ? "<description>" : strDescription) +
                    "\",\"" + (string.IsNullOrEmpty(strObjectType) ? "<objectType>" : strObjectType) +
                    "\",\"" + (string.IsNullOrEmpty(strLongName) ? "<longName>" : strLongName) +
                    "\",\"" + (string.IsNullOrEmpty(strCompositionType) ? "<compositionType>" : strCompositionType) +
                    "\",\"" + (string.IsNullOrEmpty(strElevationOfRefHeight) ? "<elevationOfRefHeight>" : strElevationOfRefHeight) +
                    "\",\"" + (string.IsNullOrEmpty(strElevationOfTerrain) ? "<elevationOfTerrain>" : strElevationOfTerrain) +
                    "\",(" + strIfcType + ")";

            if ((ifcParent != null) && (ifcParent.treeNode != null))
            {
                ifcItem.treeNode = ifcParent.treeNode.Nodes.Add(strItemText);
            }
            else
            {
                ifcItem.treeNode = ifcTree.Nodes.Add(strItemText);
            }

            ifcDict.Add(strGlobalId, new IFCItem(ifcBuildingInfos));
            //MessageBox.Show(IFCItemToString(new IFCItem(ifcBuildingInfos)));
        }

        /// <summary>
        /// 用来创建IfcBuildingStorey节点的方法
        /// </summary>
        /// <param name="ifcParent"></param>
        /// <param name="ifcItem"></param>
        private void CreateIfcBuildingStoreyTreeItem(IFCTreeItem ifcParent, IFCTreeItem ifcItem) {
            //存放头信息属性的字典
            Dictionary<string, string> ifcBuildingStoreyInfos = new Dictionary<string, string>();
            string attributeName = "";
            string attribute = "";

            int entity = IfcEngine.x86.sdaiGetInstanceType(ifcItem.instance);
            IntPtr entityNamePtr = IntPtr.Zero;
            IfcEngine.x86.engiGetEntityName(entity, IfcEngine.x86.sdaiUNICODE, out entityNamePtr);
            string strIfcType = Marshal.PtrToStringUni(entityNamePtr);
            attributeName = "IfcType";
            attribute = strIfcType;
            ifcBuildingStoreyInfos.Add(attributeName, attribute);

            IntPtr globalId;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "GlobalID", IfcEngine.x86.sdaiUNICODE, out globalId);
            string strGlobalId = Marshal.PtrToStringUni(globalId);
            attributeName = "GlobalID";
            attribute = strGlobalId;
            ifcBuildingStoreyInfos.Add(attributeName, attribute);

            IntPtr name;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Name", IfcEngine.x86.sdaiUNICODE, out name);
            string strName = Marshal.PtrToStringUni(name);
            attributeName = "Name";
            attribute = strName;
            ifcBuildingStoreyInfos.Add(attributeName, attribute);

            IntPtr description;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Description", IfcEngine.x86.sdaiUNICODE, out description);
            string strDescription = Marshal.PtrToStringUni(description);
            attributeName = "Description";
            attribute = strDescription;
            ifcBuildingStoreyInfos.Add(attributeName, attribute);

            IntPtr objectType;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "ObjectType", IfcEngine.x86.sdaiUNICODE, out objectType);
            string strObjectType = Marshal.PtrToStringUni(objectType);
            attributeName = "ObjectType";
            attribute = strObjectType;
            ifcBuildingStoreyInfos.Add(attributeName, attribute);

            IntPtr longName;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "LongName", IfcEngine.x86.sdaiUNICODE, out longName);
            string strLongName = Marshal.PtrToStringUni(longName);
            attributeName = "LongName";
            attribute = strLongName;
            ifcBuildingStoreyInfos.Add(attributeName, attribute);

            IntPtr compositionType;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "CompositionType", IfcEngine.x86.sdaiUNICODE, out compositionType);
            string strCompositionType = Marshal.PtrToStringUni(compositionType);
            attributeName = "CompositionType";
            attribute = strCompositionType;
            ifcBuildingStoreyInfos.Add(attributeName, attribute);

            IntPtr elevation;
            IfcEngine.x86.sdaiGetAttrBN(ifcItem.instance, "Elevation", IfcEngine.x86.sdaiUNICODE, out elevation);
            string strElevation = Marshal.PtrToStringUni(elevation);
            attributeName = "Elevation";
            attribute = strElevation;
            ifcBuildingStoreyInfos.Add(attributeName, attribute);

            string strItemText = "\"" + strGlobalId + "\",\"" + (string.IsNullOrEmpty(strName) ? "<name>" : strName) +
                    "\",\"" + (string.IsNullOrEmpty(strDescription) ? "<description>" : strDescription) +
                    "\",\"" + (string.IsNullOrEmpty(strObjectType) ? "<objectType>" : strObjectType) +
                    "\",\"" + (string.IsNullOrEmpty(strLongName) ? "<longName>" : strLongName) +
                    "\",\"" + (string.IsNullOrEmpty(strCompositionType) ? "<compositionType>" : strCompositionType) +
                    "\",\"" + (string.IsNullOrEmpty(strElevation) ? "<elevation>" : strElevation) +
                    "\",(" + strIfcType + ")";

            if ((ifcParent != null) && (ifcParent.treeNode != null))
            {
                ifcItem.treeNode = ifcParent.treeNode.Nodes.Add(strItemText);
            }
            else
            {
                ifcItem.treeNode = ifcTree.Nodes.Add(strItemText);
            }

            ifcDict.Add(strGlobalId, new IFCItem(ifcBuildingStoreyInfos));
            //MessageBox.Show(IFCItemToString(new IFCItem(ifcBuildingStoreyInfos)));
        }

        #region helper
        /// <summary>
        /// Helper 获取Item的类型
        /// </summary>
        /// <param name="iInstance"></param>
        /// <returns></returns>
        private string GetItemType(int iInstance)
        {
            int entity = IfcEngine.x86.sdaiGetInstanceType(iInstance);
            IntPtr entityNamePtr = IntPtr.Zero;
            IfcEngine.x86.engiGetEntityName(entity, IfcEngine.x86.sdaiSTRING, out entityNamePtr);
            return Marshal.PtrToStringAnsi(entityNamePtr);
        }

        /// <summary>
        /// Helper 将IFCITem转化为String
        /// </summary>
        /// <param name="ifcItem"></param>
        /// <returns></returns>
        private string IFCItemToString(IFCItem ifcItem) {
            StringBuilder sb = new StringBuilder();
            Dictionary<string, string> attributes = ifcItem.getAttributes();
            foreach(string key in attributes.Keys){
                sb.Append(key).Append(":").Append(attributes[key]).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Helper 判断一个实例是不是某种类型的一个实例
        /// </summary>
        /// <param name="iInstance"></param>
        /// <param name="strType"></param>
        /// <returns></returns>
        private bool IsInstanceOf(int iInstance, string strType)
        {
            return IfcEngine.x86.sdaiGetInstanceType(iInstance) == IfcEngine.x86.sdaiGetEntity(ifcModel, strType) ? true : false;
        }
        #endregion
    }
}
