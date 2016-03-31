using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.Linq;
using System.Xml;
using System.Data.OleDb;
using System.IO;
using System.Xml.XPath;
using System.Xml.Xsl;
using Limilabs.Mail;
using Limilabs.Client.POP3;
using Limilabs.Mail.MIME;
using System.Security.Cryptography;
namespace RFE
{
    class Archivo
    {
        public String nameOfPDFFile;
        public String nameOfXMLFile;
        private String saveDirectory = "C:\\facturas\\";
        private String backupDirectory = "C:\\facturasDown\\";
        private String originalsDirectory = "C:\\facturas\\Originales\\";
        public Archivo(String uuid)
        {
            //Generates name of the files according to the id that recevies plus the extension
            nameOfXMLFile = uuid + ".xml";
            nameOfPDFFile = uuid + ".pdf";
        }
        public Archivo()
        {
        }
        public String hasPDFForAttachment(String uuid)
        {
            if(File.Exists(backupDirectory+uuid+".pdf")){
                return backupDirectory + uuid + ".pdf";
            }else{
                return null;
            }
        }
        public void deleteXMLFile()
        {
            if(File.Exists(saveDirectory+nameOfXMLFile)){
                File.Delete(saveDirectory+nameOfXMLFile);
            }
            if (File.Exists(backupDirectory + nameOfXMLFile))
            {
                File.Delete(backupDirectory + nameOfXMLFile);
            }
            if (File.Exists(originalsDirectory + nameOfXMLFile))
            {
                File.Delete(originalsDirectory + nameOfXMLFile);
            }
        }
        public void renameXML(String originalNameOfFile)
        {
            //Renames XML fiel on the processing directory and on the original folder, for later FTP transport
            if (!File.Exists(saveDirectory + nameOfXMLFile))
            {
                File.Copy(originalNameOfFile, originalsDirectory + nameOfXMLFile);
                File.Copy(originalNameOfFile, saveDirectory + nameOfXMLFile);
                File.Delete(originalNameOfFile);
            }
            else
            {
                File.Delete(originalNameOfFile);
            }
        }
        public void renamePDF(String originalNameOfFile)
        {
            if (!File.Exists(saveDirectory + nameOfPDFFile))
            {
                File.Copy(originalNameOfFile, saveDirectory + nameOfPDFFile);
                File.Delete(originalNameOfFile);
            }
            else
            {
                File.Delete(originalNameOfFile);
            }
        }
        public void saveAttachments(IMail email){
            foreach (MimeData mimeFile in email.Attachments)
            {
                if (mimeFile.SafeFileName.ToLower().Contains(".xml") || mimeFile.SafeFileName.ToLower().Contains(".pdf"))
                {
                    mimeFile.Save(saveDirectory + "sin\\" + mimeFile.SafeFileName);

                }
            }
        }
        public void clearAllDirectories()
        {
            //Deletes all the past files that where already processed
            foreach (String nameOfFile in Directory.GetFiles(saveDirectory + "sin\\"))
            {
                try
                {
                    File.Delete(nameOfFile);
                }
                catch (Exception)
                {
                }
            }
            foreach (String nameOfFile in Directory.GetFiles(originalsDirectory))
            {
                try
                {
                    File.Delete(nameOfFile);
                }
                catch (Exception)
                {
                }
            }
            foreach (String nameOfFile in Directory.GetFiles(saveDirectory))
            {
                try
                {
                    File.Delete(nameOfFile);
                }
                catch (Exception)
                {
                }
            }
        }
        public void clearWorkingDirectory()
        {
            foreach (String nameOfFile in Directory.GetFiles(saveDirectory+"sin\\"))
            {
                try
                {
                    File.Delete(nameOfFile);
                }
                catch (Exception)
                {
                }
            }
            
        }
        public void deletePDFFile()
        {
            if(File.Exists(saveDirectory+nameOfPDFFile)){
            File.Delete(saveDirectory+nameOfPDFFile);
            }
        }
        public void backupFiles()
        {
            if(! (File.Exists(backupDirectory+nameOfXMLFile)))
            {
                File.Copy(saveDirectory+nameOfXMLFile, backupDirectory + nameOfXMLFile);
            }
        }
       
        public Boolean hasMultipleXML()
        {
           String[] filesLower = Directory.GetFiles(saveDirectory+"sin\\","*.xml");
           String[] filesUpper = Directory.GetFiles(saveDirectory + "sin\\", "*.XML");
           if ((filesLower.Length > 1) || (filesUpper.Length > 1))
           {
               return true;
           }
           else
           {
               return false;
           }
        }
        public Boolean hasNoXML()
        {
            int filesLower = Directory.GetFiles(saveDirectory + "sin\\", "*.xml").Length;
            int filesUpper = Directory.GetFiles(saveDirectory + "sin\\", "*.XML").Length;
            if (filesUpper == 0 && filesLower == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public String[] getXMLFile()
        {
           
            return Directory.GetFiles(saveDirectory+"sin\\","*.xml");
           
        }
        public String[] getPDFFileIfExist()
        {
            int filesLower = Directory.GetFiles(saveDirectory + "sin\\", "*.pdf").Length;
            int filesUpper = Directory.GetFiles(saveDirectory + "sin\\", "*.PDF").Length;
            if (filesLower == 0 && filesUpper == 0)
            {
                return null;
            }
            else
            {
                return Directory.GetFiles(saveDirectory + "sin\\", "*.pdf");
            }
        }
        
        public String getXMLId(String name)
        {
            String uuid = null;
            try
            {
                XElement element = XElement.Load(name);
                IEnumerable<XElement> elements = element.Elements();
                foreach (XElement innerElement in elements){
                    String elementsName = innerElement.Name.ToString();
                    if(elementsName.Contains("Complemento")){
                    foreach(XElement subElement in innerElement.Elements()){
                        if (subElement.Name.ToString().Contains("TimbreFiscalDigital"))
                        {
                            uuid = subElement.Attribute("UUID").Value;
                        }
                    }

                    }
                }

                
            }
            catch (Exception)
            {
                
            }
            return uuid;
        }
        private byte[] getOriginalChain()
        {
            /*
             * Gets original chain of characters 
             * according to the current structure
             * for cfdi generation
             */
            StreamReader reader = new StreamReader(saveDirectory + nameOfXMLFile);
            XPathDocument myXPathDoc = new XPathDocument(reader);

            //Load XSL
            XslCompiledTransform myXslTrans = new XslCompiledTransform();
            myXslTrans.Load(backupDirectory+"cadenaoriginal_3_2.xslt");

            StringWriter str = new StringWriter();
            XmlTextWriter myWriter = new XmlTextWriter(str);

            //Transformation
            myXslTrans.Transform(myXPathDoc, null, myWriter);

            //result
            return Encoding.UTF8.GetBytes(str.ToString());
                       
        }
        private bool checkStamp(string stampToprocess,string certificateToprocess)
        {
            try
            {
                //Gets certificate
                X509Certificate2 createdCertificate = new X509Certificate2(Convert.FromBase64String(certificateToprocess));
                //Converts to public key
                string stringpublicKey = createdCertificate.PublicKey.Key.ToXmlString(false);
                AsymmetricAlgorithm publicKey = AsymmetricAlgorithm.Create();
                publicKey.FromXmlString(stringpublicKey);
                RSACryptoServiceProvider rsa = (RSACryptoServiceProvider)publicKey;
                rsa.PersistKeyInCsp = false;
                // Hash the data
                SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();
                byte[] data = getOriginalChain();
                byte[] sign = Convert.FromBase64String(stampToprocess);
                byte[] hash = sha.ComputeHash(data);
                //Mades SHA Digest
                return rsa.VerifyHash(hash, CryptoConfig.MapNameToOID("SHA1"), sign);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: No se pudo desencriptar el sello. ");
                Console.WriteLine(ex.Message);
                return false;

            }
        }
        public Factura getFactura()
        {
            Factura invoice = new Factura();
            String[] values = new String[5];
            string stampOriginal =  null;
            string certificateOriginal = null;
                //Gets values of the attributes that are needed
                try
                {
                    XElement rootElement = XElement.Load(saveDirectory + nameOfXMLFile);
                    if (rootElement.Attributes("total").LongCount() > 0)
                    {
                        values[3] = rootElement.Attribute("total").Value;
                    }
                    else
                    {
                        values[3] = "";
                    }
                    if (rootElement.Attributes("folio").LongCount() > 0)
                    {
                        values[4] = rootElement.Attribute("folio").Value;
                    }
                    else
                    {
                        values[4] = "";
                    }
                    if (rootElement.Attributes("certificado").LongCount() > 0)
                    {
                        certificateOriginal =rootElement.Attribute("certificado").Value;
                    }
                    if (rootElement.Attributes("sello").LongCount() > 0)
                    {
                        stampOriginal=rootElement.Attribute("sello").Value;
                    }
                    //Decyphers digital stamp and validates
                    if (stampOriginal != null && certificateOriginal != null)
                    {
                        Console.WriteLine("Validando sello y certificado...");
                        if (!checkStamp(stampOriginal, certificateOriginal))
                        {
                            Console.WriteLine("Error: Sello digital no es correcto");
                            return invoice = new Factura("Sello digital y/o certificado incorrecto(s)");
                        }
                        Console.WriteLine("Es correcto!");
                    }
                    else
                    {
                        Console.WriteLine("Error: Falta sello/certificado");
                        return invoice = new Factura("Falta sello/certificado");
                    }
                    //Looks on on the childs for the needed values of the verification chain
                    IEnumerable<XElement> allElements = rootElement.Elements();
                    foreach (XElement innerElement in allElements)
                    {
                        String elementsName = innerElement.Name.ToString();
                        if (elementsName.Contains("Emisor"))
                        {
                            values[1] = innerElement.Attribute("rfc").Value;
                        }
                        else if (elementsName.Contains("Receptor"))
                        {
                            values[0] = innerElement.Attribute("rfc").Value;
                        }
                        else if (elementsName.Contains("Complemento"))
                        {
                            foreach (XElement subElement in innerElement.Elements())
                            {
                                if (subElement.Name.ToString().Contains("TimbreFiscalDigital"))
                                {
                                    values[2] = subElement.Attribute("UUID").Value;
                                }
                            }

                        }
                        else if (elementsName.Contains("Addenda"))
                        {
                            rootElement.Element(elementsName).Remove();
                        }
                    }
                    //Saves working file
                    rootElement.Save(saveDirectory + nameOfXMLFile);
                    //Checks squema vs file layout
                    invoice.error=hasCorrectSchema();
                    if (invoice.error == null)
                    {
                        invoice = new Factura(values[0], values[1], values[2], values[3], values[4]);
                    }

                }
                catch (Exception)
                {
                    Console.WriteLine("Error: Couldn't read nodes properly");
                    invoice.error = "No se pudieron leer los atributos del XML";
                }
            
            return invoice;
        }
        private String hasCorrectSchema()
        {
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ValidationType = ValidationType.Schema;
                settings.ValidationFlags = settings.ValidationFlags | XmlSchemaValidationFlags.ProcessInlineSchema;
                settings.ValidationFlags = settings.ValidationFlags | XmlSchemaValidationFlags.ReportValidationWarnings ;
                settings.ValidationFlags = settings.ValidationFlags | XmlSchemaValidationFlags.ProcessSchemaLocation;
                using(XmlReader reader = XmlReader.Create(saveDirectory+nameOfXMLFile,settings)){
                while (reader.Read()){

                }
                    reader.Close();
                    return null;
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return exc.Message;
            }
        }
    }
}
