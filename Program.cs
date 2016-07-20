using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Limilabs.Mail;
using Limilabs.Client.POP3;
using Limilabs.Mail.MIME;
using RestSharp;
using NLog;

namespace RFE
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static String exportFileDirectory = "C:\\recepcion";
        static void checkForPendingInvoice()
        {
            ControladorBD dbAccess = new ControladorBD();
            Email mail = new Email();
            //Gets pending request for new invoice
            Factura[] pendingInvoices = dbAccess.getPendingInvoice();
            Logger.Debug("Checando solicitudes pendientes de proveedores nuevos...");
            foreach (Factura invoice in pendingInvoices)
            {
                
                //If the invoice contains info it passes
                if (invoice != null)
                {
                    Logger.Debug("Procesando solicitud nueva");
                    Solicitud request = dbAccess.getRequestMain(invoice);
                    //Checks for duplicity
                    if (!dbAccess.existenceRequest(request))
                    {
                        //Send email to requester and direct chief, according to society and department.
                        mail.subject = "Solicitud de aprobación";
                        String emailContent = "La factura No. " + request.invoiceNo + " del proveedor " + request.sender +
                        "por un total de: $" + request.total + " ha sido recibida y validada, por lo que se generó una solicitud de egresos.";
                        Logger.Debug("Agregando solicitud a BD");
                        dbAccess.insertRequest(request);
                        dbAccess.getRequesterEmail(request);
                        mail.from = request.requesterEmail;
                        Logger.Debug("Enviando correo a solicitante");
                        mail.sendComposeMail(emailContent, request.uuid, request.society.ToString());
                        //mail.sendAuthorizationEmail(emailContent, request.uuid, request.society.ToString());
                        dbAccess.getChiefEmail(request);
                        mail.from = request.chiefEmail;
                        Logger.Debug("Enviando correo a jefe directo");
                        mail.sendComposeMail(emailContent, request.uuid, request.society.ToString());
                        //mail.sendAuthorizationEmail(emailContent, request.uuid, request.society.ToString());
                        Logger.Debug("Termina con solicitud");
                        dbAccess.deletePending(request.uuid);
                    }
                    else
                    {
                        dbAccess.deletePending(request.uuid);
                    }
                }
            }
        }
        static void Main(string[] args)
        {
            Logger.Info("Iniciando RFE");
            //Initialize classes to use
            Archivo files = new Archivo();
            Email mail = new Email();
            Logger.Debug("Preprocesando..");
            //Prepares workspace
            files.clearAllDirectories();
            Logger.Debug("Intentando conectar al correo...");
            //Attempts connection
            Pop3 mailConnection = new Pop3();
            try
            {
                mailConnection.Connect("secure.emailsrvr.com");
                mailConnection.UseBestLogin(mail.account, mail.password);
                Logger.Debug("Exito!");
                Logger.Debug("Cargando correos...");
                int emailCounter = 0;
                //Starts looking in the inbox
                using (mailConnection)
                {
                    //Checks for all the mail received
                    Logger.Info("Total de correos = " + mailConnection.GetAll().Count().ToString());
                    foreach (String mailIdentifier in mailConnection.GetAll())
                    {
                        Logger.Debug("Procesando correo no." + (emailCounter + 1).ToString());
                        //Limits the email processor quantity
                        if (emailCounter <= 20)
                        {
                            //Anti bot checking section
                            Logger.Debug("Revisando que no sea correo basura...");
                            //Internal classes used initialize
                            ControladorBD dbAccess = new ControladorBD();
                            Email internalMail = new Email(mailIdentifier, mailConnection);
                            //Checks for no bot account on the sender
                            if (internalMail.hasValidAddress())
                            {
                                Logger.Debug("Exito!");
                                //Checks for at least one attached file
                                Logger.Debug("Verificando adjuntos...");
                                if (internalMail.totalAttachments == 0)
                                {
                                    Logger.Info("Error: Cero adjuntos");
                                    dbAccess.insertErrorMissingFile(internalMail.from);
                                    Logger.Debug("Enviando correo de error...");
                                    internalMail.subject = "Error de recepción";
                                    internalMail.sendComposeMail("No se encontró ningún archivo anexo en el correo.", "", "");
                                    
                                }
                                else
                                {
                                    //Saves the attachments of the email to start working
                                    files.saveAttachments(internalMail.email);
                                    //Checks for a XML file and no more than one
                                    if (files.hasNoXML())
                                    {
                                        Logger.Info("Error: Sin XML");
                                        Logger.Debug("Enviando correo de error...");
                                        //Reports error on DB and by mail
                                        dbAccess.insertErrorMissingFile(internalMail.from);
                                        internalMail.subject = "Error de recepción";
                                        internalMail.sendComposeMail("No se encontró ningún archivo de tipo XML en el correo.", "", "");                                       
                                    }
                                    else if (files.hasMultipleXML())
                                    {
                                        //Reports error on DB and by mail
                                        dbAccess.insertErrorMoreThanOneFile(internalMail.from);
                                        Logger.Info("Error: Multiples XMLs");
                                        Logger.Debug("Enviando correo de error...");
                                        internalMail.subject = "Error de recepción";
                                        internalMail.sendComposeMail("Múltiples comprobantes en el correo.", "", "");   
                                    }
                                    else
                                    {
                                        //Read identifier from XML content
                                        Logger.Debug("Obteniendo identificador del XML...");
                                        String[] fileXml = files.getXMLFile();
                                        String actualIdentifier = files.getXMLId(fileXml[0]);
                                        //First Validation
                                        //----------------------------------------------
                                        //Structure Checks if the XML format has the UUID else send error
                                        if (actualIdentifier != null)
                                        {
                                            //Creates current names for files
                                            files = new Archivo(actualIdentifier);
                                            Logger.Debug("Exito!");
                                            files.renameXML(fileXml[0]);
                                            //Extra backup of oiginal XML
                                            files.backupFiles();
                                            Factura invoice = files.getFactura();
                                            //Second Validation
                                            //-----------------------------------------------
                                            //Structure check with required fields, valid digital stamp and correct schema
                                            if (invoice.error == null)
                                            {
                                                //Third validation
                                                //-------------------------------------------
                                                //Security check for a correct/valid RFC
                                                Logger.Debug("Validando RFC de receptor...");
                                                if (invoice.hasValidRFC())
                                                {
                                                    Logger.Debug("Exito! ");
                                                    String[] filePdf = files.getPDFFileIfExist();
                                                    //Fourth validation
                                                    //---------------------------------------
                                                    //SAT's status service online response
                                                    Logger.Debug("Revisando en SAT");
                                                    switch (invoice.statusOnSAT())
                                                    {
                                                        case 0: //Mistake at connection
                                                        case 2: //Pending
                                                        case 1: //Successful
                                                            dbAccess.insertReceived(internalMail.from, invoice.uuid);
                                                            dbAccess.insertPending(files.nameOfXMLFile, invoice.recepientRFC, invoice.senderRFC, invoice.folio);
                                                            //Get PDF file if exists and rename with the id of the invoice
                                                            if (filePdf != null)
                                                            {
                                                                files.renamePDF(filePdf[0]);
                                                            }
                                                            Logger.Info("Exito! Todas las validaciones pasaron.");

                                                            break;
                                                        case 3://Canceled
                                                            //Reports error on DB and by mail
                                                            dbAccess.insertCanceled(files.nameOfXMLFile, invoice.recepientRFC, invoice.senderRFC, invoice.folio);
                                                            Logger.Info("Error: Comprobante cancelado");
                                                            Logger.Debug("Enviando correo de error...");
                                                            internalMail.subject = "Error de recepción";
                                                            internalMail.sendComposeMail("El comprobante con UUID: '" + 
                                                                invoice.uuid + "' que mando NO fue aceptado, debido a que se encuentra cancelado ante el SAT.", "", "");   
                                                            files.deleteXMLFile();
                                                            files.clearWorkingDirectory();
                                                            break;
                                                        case 4://Incorrect
                                                            //Reports error on DB and by mail
                                                            dbAccess.insertErrorIncorrectInformation(mail.from);
                                                            Logger.Info("Error: Comprobante incorrecto ante el SAT");
                                                            Logger.Debug("Enviando correo de error...");
                                                            internalMail.subject = "Error de recepción";
                                                            internalMail.sendComposeMail("El comprobante que mando NO fue aceptado, debido a que el SAT " +
                                                            "marca incoherencia en uno o más de los siguientes campos: RFC Receptor, RFC Emisor, Total, UUID", "", ""); 
                                                            files.deleteXMLFile();
                                                            files.clearWorkingDirectory();
                                                            break;
                                                        default:
                                                            break;

                                                    }
                                                }
                                                else
                                                {
                                                    //Reports error on DB and by mail
                                                    dbAccess.insertErrorIncorrectInformation(internalMail.from);
                                                    Logger.Info("Error: RFC incorrecto");
                                                    Logger.Debug("Enviando correo de error...");
                                                    internalMail.subject = "Error de recepción";
                                                    internalMail.sendComposeMail("El valor para el RFC del receptor en el archivo con UUID: '" + 
                                                        invoice.uuid + "' no es válido para ninguna de nuestras empresas", "", ""); 
                                                    //Cleans workspace and already modified files
                                                    files.deleteXMLFile();
                                                    files.clearWorkingDirectory();
                                                }

                                            }
                                            else
                                            {
                                                //Reports error on DB and by mail
                                                //Checks for details on the mistake found on the file
                                                if (invoice.invalidStamp == 0)
                                                {
                                                    dbAccess.insertErrorIncorrectInformation(internalMail.from);
                                                }
                                                else
                                                {
                                                    dbAccess.insertErrorIncorrectStamp(internalMail.from);
                                                }
                                                Logger.Info("Error: Faltan campos en el XML");
                                                Logger.Debug("Enviando correo de error...");
                                                internalMail.subject = "Error de recepción";
                                                internalMail.sendComposeMail("Faltas al Anexo 20, faltas al esquema del archivo. '" + 
                                                    invoice.error + "'", "", "");
                                                //Cleans workspace and already modified files
                                                files.deleteXMLFile();
                                                files.clearWorkingDirectory();

                                            }

                                        }
                                        else
                                        {
                                            //Reports error on DB and by mail
                                            dbAccess.insertErrorIncorrectInformation(internalMail.from);
                                            Logger.Info("Error: No se pudo leer el XML");
                                            Logger.Debug("Enviando correo de error...");
                                            internalMail.subject = "Error de recepción";
                                            internalMail.sendComposeMail("Imposible leer el archivo XML. No se pudo obtener el UUID.", "", ""); 
                                            files.clearWorkingDirectory();

                                        }
                                    }

                                }
                            }
                            //Erases actual finished mail
                            Logger.Debug("Borrando correo del servidor...");
                            internalMail.eraseEmail(mailIdentifier, mailConnection);
                            //Cleans workspace
                            files.clearWorkingDirectory();
                            Logger.Debug("Siguiente correo");
                            emailCounter++;
                        }
                        else { Logger.Info("Se excede del limite de correos a procesar. Re agendando"); break; }

                    }
                    mailConnection.Close();
                }
            }
            catch (Exception)
            {
                //In case of an incorrect connection to the email server
                //it just re-schedules the task
                Logger.Info("Error: No se pudo conectar al servidor del correo");
                Logger.Debug("Re agendando tarea...");
            }
            Logger.Info("Fin de receptor");
            
            //Starts second section of request-invoice
            Logger.Info("Inicia revision de solicitudes pendientes...");
            checkForPendingInvoice();
            Logger.Info("Finaliza proceso.");
            //Starts exporting files and information to the ftp server and database
            //calling the external batch file for that
            Logger.Info("Inicia exportación");
            System.Diagnostics.Process exportingProcess = new System.Diagnostics.Process();
            exportingProcess.StartInfo.FileName = exportFileDirectory + "\\import2.cmd";
            exportingProcess.StartInfo.WorkingDirectory = exportFileDirectory;
            exportingProcess.Start();
            Logger.Info("Finaliza exportación");

        }
        
    }
}
