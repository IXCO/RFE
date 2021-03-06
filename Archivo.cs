﻿using System;
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
using System.Web;
using System.Xml.XPath;
using System.Xml.Xsl;
using Limilabs.Mail;
using Limilabs.Client.POP3;
using Limilabs.Mail.MIME;
using System.Security.Cryptography;
using NLog;
namespace RFE
{
    class Archivo
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
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
                catch (Exception ex)
                {
                    Logger.Warn("Error al guardar archivo");
                    Logger.Warn(ex.Message);
                }
            }
            foreach (String nameOfFile in Directory.GetFiles(originalsDirectory))
            {
                try
                {
                    File.Delete(nameOfFile);
                }
                catch (Exception ex)
                {
                    Logger.Warn("Error al guardar archivo");
                    Logger.Warn(ex.Message);
                }
            }
            foreach (String nameOfFile in Directory.GetFiles(saveDirectory))
            {
                try
                {
                    File.Delete(nameOfFile);
                }
                catch (Exception ex)
                {
                    Logger.Warn("Error al guardar archivo");
                    Logger.Warn(ex.Message);
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
                catch (Exception ex)
                {
                    Logger.Warn("Error al limpiar directorio");
                    Logger.Warn(ex.Message);
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
            catch (Exception ex)
            {
                Logger.Warn("Error: No se pudo leer el UUID. " + ex.Message);
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
            //Decodifier
            StringWriter decodedstr = new StringWriter();
            HttpUtility.HtmlDecode(str.ToString(), decodedstr);
            var resulttostring = decodedstr.ToString();
            //result
            return Encoding.UTF8.GetBytes(resulttostring);
                       
        }
        private bool checkStamp(string stampToprocess,string certificateToprocess)
        {
            /* Proceess of decypher
             *      DecryptBase64(certificate)
             *    + Convert to X509 certificate
             *    -------------------------------
             *    = Original certificate
             *    
             *      Orginal certificate
             *    + Asymetric Algorithm
             *    -------------------------------
             *    = Public key
             *    
             *      SHA HASH
             *    + Original chain
             *    -------------------------------
             *    = Original Hashing Data
             *    
             *      DecryptBase64(Original Stamp)
             *    ------------------------------
             *    = Signature
             *    
             *      Original Hashing Data
             *    + Signature
             *      RSA(Public key).SHA1
             *    ------------------------------    
             *    = valid? 
             * 
             */
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
                Logger.Warn("Error: No se pudo desencriptar el sello. ");
                Logger.Warn(ex.Message);
                return false;

            }
        }
        private String checkDigitalStamp(String stampInput, String certificateInput)
        {
            //Checks for existence
            if (stampInput != null && certificateInput != null)
            {
                Logger.Debug("Validando sello y certificado...");
                //Checks for validation and consistency of the cipher stamp and certificate
                if (!checkStamp(stampInput, certificateInput))
                {
                    Logger.Warn("Error: Sello digital no es correcto");
                    return "Sello digital y/o certificado incorrecto(s)";
                }
                else
                {
                    Logger.Debug("Es correcto!");
                    return null;
                }
            }
            else
            {
                Logger.Warn("Error: Falta sello/certificado");
                return "Falta sello/certificado";
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
                    invoice.error = checkDigitalStamp(stampOriginal, certificateOriginal);
                    //Looks on on the child nodes for the needed values of the verification chain
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
                            //Removes unused optional node
                            rootElement.Element(elementsName).Remove();
                        }
                    }
                    //Saves working file
                    rootElement.Save(saveDirectory + nameOfXMLFile);
                    //Checks squema vs file layout
                    if (invoice.error == null)
                    {
                        invoice.error = hasCorrectSchema();
                    }
                    else
                    {
                        invoice.invalidStamp = 1;
                    }
                    if (invoice.error == null)
                    {
                        invoice = new Factura(values[0], values[1], values[2], values[3], values[4]);
                    }

                }
                catch (Exception ex)
                {
                    Logger.Warn("Error: No se pudieron leer los nodos correctamente");
                    invoice.error = "No se pudieron leer los atributos del XML. Especificación tecnica: "+ex.Message;
                }
            
            return invoice;
        }
        private String hasCorrectSchema()
        {
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ValidationType = ValidationType.Schema;
                //Gets namespace validation schema from the XML file
                settings.ValidationFlags = settings.ValidationFlags | XmlSchemaValidationFlags.ProcessInlineSchema;
                settings.ValidationFlags = settings.ValidationFlags | XmlSchemaValidationFlags.ReportValidationWarnings ;
                settings.ValidationFlags = settings.ValidationFlags | XmlSchemaValidationFlags.ProcessSchemaLocation;
                //Reads file and process correct layout
                using(XmlReader reader = XmlReader.Create(saveDirectory+nameOfXMLFile,settings)){
                while (reader.Read()){

                }
                    reader.Close();
                    return null;
                }
            }
            catch (Exception exc)
            {
                Logger.Warn(exc.Message);
                return "Especificación tecnica: "+exc.Message;
            }
        }
    }
}
