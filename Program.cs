﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Limilabs.Mail;
using Limilabs.Client.POP3;
using Limilabs.Mail.MIME;
using RestSharp;

namespace RFE
{
    class Program
    {
        private static String exportFileDirectory = "C:\\recepcion";
        static void checkForPendingInvoice()
        {
            ControladorBD dbAccess = new ControladorBD();
            Email mail = new Email();
            //Gets pending request for new invoice
            Factura[] pendingInvoices = dbAccess.getPendingInvoice();
            Console.WriteLine("Checando solicitudes pendientes de proveedores nuevos...");
            foreach (Factura invoice in pendingInvoices)
            {
                
                //If the invoice contains info it passes
                if (invoice != null)
                {
                    Console.WriteLine("Procesando solicitud nueva");
                    Solicitud request = dbAccess.getRequestMain(invoice);
                    //Checks for duplicity
                    if (!dbAccess.existenceRequest(request))
                    {
                        //Send email to requester and direct chief, according to society and department.
                        mail.subject = "Solicitud de aprobación";
                        String emailContent = "La factura No. " + request.invoiceNo + " del proveedor " + request.sender +
                        "por un total de: $" + request.total + " ha sido recibida y validada, por lo que se generó una solicitud de egresos.";
                        Console.WriteLine("Agregando solicitud a BD");
                        dbAccess.insertRequest(request);
                        dbAccess.getRequesterEmail(request);
                        mail.from = request.requesterEmail;
                        Console.WriteLine("Enviando correo a solicitante");
                        //IRestResponse response = mail.sendComposeMail(emailContent, request.uuid, request.society.ToString());
                        //if (response.Content.Contains("Queued"))
                        //{
                        //}
                        mail.sendAuthorizationEmail(emailContent, request.uuid, request.society.ToString());
                        dbAccess.getChiefEmail(request);
                        mail.from = request.chiefEmail;
                        Console.WriteLine("Enviando correo a jefe directo");
                        mail.sendAuthorizationEmail(emailContent, request.uuid, request.society.ToString());
                        Console.WriteLine("Termina con solicitud");
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
            Console.WriteLine("Iniciando RFE");
            //Initialize classes to use
            Archivo files = new Archivo();
            Email mail = new Email();
            Console.WriteLine("Preprocesando..");
            //Prepares workspace
            files.clearAllDirectories();
            Console.WriteLine("Intentando conectar al correo...");
            //Attempts connection
            Pop3 mailConnection = new Pop3();
            try
            {
                mailConnection.Connect("secure.emailsrvr.com");
                mailConnection.UseBestLogin(mail.account, mail.password);
                Console.Write("Exito!\n");
                Console.WriteLine("Cargando correos...");
                int emailCounter = 0;
                //Starts looking in the inbox
                using (mailConnection)
                {
                    //Checks for all the mail received
                    Console.WriteLine("Total de correos = " + mailConnection.GetAll().Count().ToString());
                    foreach (String mailIdentifier in mailConnection.GetAll())
                    {
                        Console.WriteLine("Procesando correo no." + (emailCounter + 1).ToString());
                        //Limits the email processor quantity
                        if (emailCounter <= 20)
                        {
                            //Anti bot checking section
                            Console.Write("Revisando que no sea correo basura...");
                            //Internal classes used initialize
                            ControladorBD dbAccess = new ControladorBD();
                            Email internalMail = new Email(mailIdentifier, mailConnection);
                            //Checks for no bot account on the sender
                            if (internalMail.hasValidAddress())
                            {
                                Console.Write("Exito!\n");
                                //Checks for at least one attached file
                                Console.WriteLine("Verificando adjuntos...");
                                if (internalMail.totalAttachments == 0)
                                {
                                    Console.WriteLine("Error: Cero adjuntos");
                                    dbAccess.insertErrorMissingFile(internalMail.from);
                                    Console.WriteLine("Enviando correo de error...");
                                    internalMail.subject = "Error de recepción";
                                    internalMail.sendErrorEmail("No se encontró ningún archivo anexo en el correo.");
                                }
                                else
                                {
                                    //Saves the attachments of the email to start working
                                    files.saveAttachments(internalMail.email);
                                    //Checks for XML file and no more than one
                                    if (files.hasNoXML())
                                    {
                                        Console.WriteLine("Error: Sin XML");
                                        Console.WriteLine("Enviando correo de error...");
                                        //Reports error on DB and by mail
                                        dbAccess.insertErrorMissingFile(internalMail.from);
                                        internalMail.subject = "Error de recepción";
                                        //IRestResponse response = internalMail.sendComposeMail("", "", "");
                                        //if (response.Content.Contains("Queued"))
                                        //{
                                        //}
                                        
                                        internalMail.sendErrorEmail("No se encontró ningún archivo de tipo XML en el correo.");

                                    }
                                    else if (files.hasMultipleXML())
                                    {
                                        //Reports error on DB and by mail
                                        dbAccess.insertErrorMoreThanOneFile(internalMail.from);
                                        Console.WriteLine("Error: Multiples XMLs");
                                        Console.WriteLine("Enviando correo de error...");
                                        internalMail.subject = "Error de recepción";
                                        internalMail.sendErrorEmail("Múltiples comprobantes en el correo.");

                                    }
                                    else
                                    {
                                        //Read identifier from XML content
                                        Console.Write("Obteniendo identificador del XML...");
                                        String[] fileXml = files.getXMLFile();
                                        String actualIdentifier = files.getXMLId(fileXml[0]);
                                        //First Validation
                                        //----------------------------------------------
                                        //Structure Checks if the XML format has the UUID else send error
                                        if (actualIdentifier != null)
                                        {
                                            //Creates current names for files
                                            files = new Archivo(actualIdentifier);
                                            Console.Write("Exito! \n");
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
                                                Console.Write("Validando RFC de receptor...");
                                                if (invoice.hasValidRFC())
                                                {
                                                    Console.Write("Exito! \n");
                                                    String[] filePdf = files.getPDFFileIfExist();
                                                    //Fourth validation
                                                    //---------------------------------------
                                                    //SAT's service online response
                                                    Console.WriteLine("Revisando en SAT");
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
                                                            Console.WriteLine("Exito! Todas las validaciones pasaron.");

                                                            break;
                                                        case 3://Canceled
                                                            //Reports error on DB and by mail
                                                            dbAccess.insertCanceled(files.nameOfXMLFile, invoice.recepientRFC, invoice.senderRFC, invoice.folio);
                                                            Console.WriteLine("Error: Comprobante cancelado");
                                                            Console.WriteLine("Enviando correo de error...");
                                                            internalMail.subject = "Error de recepción";
                                                            internalMail.sendErrorEmail("El comprobante con UUID: '"+invoice.uuid+"' que mando NO fue aceptado, debido a que se encuentra cancelado ante el SAT.");
                                                            files.deleteXMLFile();
                                                            files.clearWorkingDirectory();
                                                            break;
                                                        case 4://Incorrect
                                                            //Reports error on DB and by mail
                                                            dbAccess.insertErrorIncorrectInformation(mail.from);
                                                            Console.WriteLine("Error: Comprobante incorrecto ante el SAT");
                                                            Console.WriteLine("Enviando correo de error...");
                                                            internalMail.subject = "Error de recepción";
                                                            internalMail.sendErrorEmail("El comprobante que mando NO fue aceptado, debido a que el SAT "+
                                                            "marca incoherencia en uno o más de los siguientes campos: RFC Receptor, RFC Emisor, Total, UUID");
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
                                                    Console.WriteLine("Error: RFC incorrecto");
                                                    Console.WriteLine("Enviando correo de error...");
                                                    internalMail.subject = "Error de recepción";
                                                    internalMail.sendErrorEmail("El valor para el RFC del receptor en el archivo con UUID: '" + invoice.uuid + "' no es válido para ninguna de nuestras empresas");
                                                    //Cleans workspace and already modified files
                                                    files.deleteXMLFile();
                                                    files.clearWorkingDirectory();
                                                }

                                            }
                                            else
                                            {
                                                //Reports error on DB and by mail
                                                if (invoice.invalidStamp == 0)
                                                {
                                                    dbAccess.insertErrorIncorrectInformation(internalMail.from);
                                                }
                                                else
                                                {
                                                    dbAccess.insertErrorIncorrectStamp(internalMail.from);
                                                }
                                                Console.WriteLine("Error: Faltan campos en el XML");
                                                Console.WriteLine("Enviando correo de error...");
                                                internalMail.subject = "Error de recepción";
                                                internalMail.sendErrorEmail("Faltas al Anexo 20, faltas al esquema del archivo. '" + invoice.error + "'");
                                                files.deleteXMLFile();
                                                files.clearWorkingDirectory();

                                            }

                                        }
                                        else
                                        {
                                            //Reports error on DB and by mail
                                            dbAccess.insertErrorIncorrectInformation(internalMail.from);
                                            Console.WriteLine("Error: No se pudo leer el XML");
                                            Console.WriteLine("Enviando correo de error...");
                                            internalMail.subject = "Error de recepción";
                                            internalMail.sendErrorEmail("Imposible leer el archivo XML. No se pudo obtener el UUID.");
                                            files.clearWorkingDirectory();

                                        }
                                    }

                                }
                            }
                            //Erases actual finished mail
                            Console.WriteLine("Borrando correo del servidor...");
                            internalMail.eraseEmail(mailIdentifier, mailConnection);
                            //Cleans workspace
                            files.clearWorkingDirectory();
                            Console.WriteLine("Siguiente correo");
                            emailCounter++;
                        }
                        else { Console.WriteLine("Se excede del limite de correos a procesar. Re agendando"); break; }

                    }
                    mailConnection.Close();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error: No se pudo conectar al servidor del correo");
                Console.WriteLine("Re agendando tarea...");
            }
            Console.WriteLine("Fin de receptor");
            
            //Starts second section of request-invoice
            Console.WriteLine("Inicia revision de solicitudes pendientes...");
            checkForPendingInvoice();
            Console.WriteLine("Finaliza proceso.");
            //Starts exporting files and information to the ftp server and database
            Console.WriteLine("Inicia exportación");
            System.Diagnostics.Process exportingProcess = new System.Diagnostics.Process();
            exportingProcess.StartInfo.FileName = exportFileDirectory + "\\import2.cmd";
            exportingProcess.StartInfo.WorkingDirectory = exportFileDirectory;
            exportingProcess.Start();
            Console.WriteLine("Finaliza exportación");
        }
        
    }
}
