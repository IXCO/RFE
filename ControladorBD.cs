using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
namespace RFE
{
    class ControladorBD
    {

        private MySqlConnection connection;
        public ControladorBD()
        {
            connection = new MySqlConnection();
            connection.ConnectionString="server ="+server
                +";user id="+user+";password="+password
                +";database="+database;
        }
        public Boolean deletePending(String idInvoice)
        {
            connection.Open();
            Boolean success = true;
            String statement = "DELETE FROM pendientes_de_solicitud WHERE uuid ='" +idInvoice + "';";
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (MySqlException)
            {
                success = false;
            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return success;
        }
        public Solicitud getRequestMain(Factura invoice)
        {
            Solicitud request = null;
            String statement = "SELECT soc_fkey,dep_fkey,EMISOR.NOMBRE,COMPROBANTE.FOLIO,COMPROBANTE.TOTAL FROM provxsociedad " +
                "INNER JOIN CFDI_EMISOR EMISOR ON EMISOR.RFC = '" + invoice.senderRFC + "' " +
                "INNER JOIN CFDI_COMPROBANTE COMPROBANTE ON EMISOR.CFDI_COMPROBANTE_FKEY = COMPROBANTE.CFDI_COMPROBANTE_PKEY " +
                "INNER JOIN CFDI_COMPLEMENTO COMPLEMENTO ON COMPLEMENTO.CFDI_COMPROBANTE_FKEY = COMPROBANTE.CFDI_COMPROBANTE_PKEY " +
                "INNER JOIN TFD_TIMBREFISCALDIGITAL TIMBRE ON TIMBRE.CFDI_COMPLEMENTO_FKEY = COMPLEMENTO.CFDI_COMPLEMENTO_PKEY " +
                "INNER JOIN sociedades ON sociedades.id_soc = soc_fkey WHERE sociedades.rfc = '" +
                invoice.recepientRFC + "' AND rfcE='" + invoice.senderRFC + "' AND TIMBRE.UUID= '" + invoice.uuid + "' group by EMISOR.RFC;";
            connection.Open();
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    if(reader.IsDBNull(3)){
                        request = new Solicitud(reader.GetInt32(0),reader.GetInt32(1).ToString(),reader.GetString(2),reader.GetString(4));
                    }else{
                        request = new Solicitud(reader.GetInt32(0),reader.GetInt32(1).ToString(),reader.GetString(2),reader.GetString(4),reader.GetString(3));
                    }
                }
                request.uuid= invoice.uuid;
            }
            catch (MySqlException)
            {

            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            
            return request;
        }

        public void getChiefEmail(Solicitud request)
        {
            String statement =  "SELECT us.correo FROM sociedad soc " +
                "INNER JOIN departamento dep ON dep.dep_pkey = soc.dep_fkey " +
                "INNER JOIN usuario_permisos USER ON USER.us_fkey = dep.us_fkey " +
                "INNER JOIN usuario us ON us.us_pkey = USER.us_fkey " +
                "WHERE soc.soc_fkey = " + request.society.ToString() + " And USER.valor_fkey = 2 And dep.nombre = " + request.department + ";";
            connection.Open();
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    request.chiefEmail = reader.GetString(0);
                   
                }
            }
            catch (MySqlException)
            {

            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
        }
        public void getRequesterEmail(Solicitud request)
        {
            String statement = "SELECT DISTINCT user.correo,dep.nombre FROM usuario user " +
                "INNER JOIN departamento dep ON dep.us_fkey= user.us_pkey " +
                "INNER JOIN usuario_permisos per ON per.us_fkey = user.us_pkey "+
                "WHERE per.valor_fkey = 3 And dep.dep_pkey = " + request.department+ " ;";
            connection.Open();
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    request.requesterEmail = reader.GetString(0);
                    request.department = reader.GetString(1);
                }
            }
            catch (MySqlException)
            {

            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
        }
        public Factura[] getPendingInvoice(){
            Factura[] invoiceArray = new Factura[5];
            int index = 0;
            String statement ="SELECT p.uuid, p.rfc_emisor, v.RFC" +
            " FROM pendientes_de_solicitud p" +
            " INNER JOIN provxsociedad s ON s.rfcE = p.rfc_emisor" +
            " INNER JOIN rfc_validos v ON s.rfc_r = v.ID" +
            " INNER JOIN TFD_TIMBREFISCALDIGITAL TIMBRE ON TIMBRE.UUID = p.uuid" +
            " INNER JOIN CFDI_COMPLEMENTO COMPLEMENTO ON TIMBRE.CFDI_COMPLEMENTO_FKEY = COMPLEMENTO.CFDI_COMPLEMENTO_PKEY" +
            " INNER JOIN CFDI_COMPROBANTE COMPROBANTE ON COMPROBANTE.CFDI_COMPROBANTE_PKEY = COMPLEMENTO.CFDI_COMPROBANTE_FKEY" +
            " INNER JOIN CFDI_RECEPTOR RECEPTOR ON RECEPTOR.CFDI_COMPROBANTE_FKEY = COMPROBANTE.CFDI_COMPROBANTE_PKEY" +
            " WHERE v.RFC = RECEPTOR.RFC ORDER BY TIMBRE.UUID LIMIT 5;";
            connection.Open();
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                MySqlDataReader reader = command.ExecuteReader();
                
                while (reader.Read())
                {
                    invoiceArray[index] = new Factura(reader.GetString(2),reader.GetString(1),reader.GetString(0));
                    index++;
                }
            }
            catch (MySqlException)
            {
                
            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return invoiceArray;
        }

        public Boolean existenceRequest(Solicitud request)
        {
            Boolean exist = false;
            String statement = "SELECT * FROM factura WHERE uuid_fkey='" + request.uuid + "' ;";
            connection.Open();
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    exist = true;
                }

            }
            catch (MySqlException)
            {

            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return exist;
        }
        public Boolean rfcExist(String rfc)
        {
            bool exist = false;
            String statement = "SELECT * FROM rfc_validos WHERE RFC='"+rfc+"';";
            connection.Open();
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    exist = true;
                }

            }
            catch (MySqlException)
            {

            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return exist;
        }

        public void insertRequest(Solicitud request)
        {
            connection.Open();
            String statement = "INSERT INTO factura (uuid_fkey, soc_fkey) VALUES('" + request.uuid + "'," + request.society.ToString()+ ");";
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (MySqlException)
            {            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
        }
        public Boolean insertPending(String name, String receiverRFC, String senderRFC, String serial)
        {
            bool success = true;
            connection.Open();
            String statement = "INSERT INTO Pendientes (name,rfcR,rfcE,folio,error,time_added) VALUES('" + name + "','" + receiverRFC + "','" + senderRFC + "','" + serial + "','Pendiente','" + DateTime.Today.ToShortDateString() + "');";
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (MySqlException)
            {
                success = false;
            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return success;
        }
        public Boolean insertReceived(String senderAddress,String nameOfInvoice)
        {
            bool success = true;
            connection.Open();
            String statement = "INSERT INTO Recibidas (nombreProv,idfact,fechaValida) VALUES('" + senderAddress + "','" + nameOfInvoice + "','" + DateTime.Today.ToShortDateString() + "');";
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (MySqlException)
            {
                success = false;
            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return success;
        }
        public Boolean insertCanceled(String name,String receiverRFC,String senderRFC,String serial)
        {
            bool success = true;
            connection.Open();
            String statement = "INSERT INTO Canceladas (name,rfcR,rfcE,folio,time_added) VALUES('" + name + "','" + receiverRFC + "','" + senderRFC + "',"+
                "'" + serial + "','" + DateTime.Today.ToShortDateString() + "');";
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (MySqlException)
            {
                success = false;
            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return success;
        }
        public Boolean insertErrorMissingFile(String sender)
        {
            bool success = true;
            connection.Open();
            String statement = "INSERT INTO errores_recep (tipo_error,correo,fecha)  VALUES('Sin ningun anexo','" + sender + "','"+DateTime.Today.ToShortDateString()+"');";
           
            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (MySqlException)
            {
                success = false;
            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return success;
        }
        public Boolean insertErrorMoreThanOneFile(String sender)
        {
            bool success = true;
            connection.Open();
            String statement = "INSERT INTO errores_recep (tipo_error,correo,fecha)  VALUES('Mas de un XML','" + sender + "','" + DateTime.Today.ToShortDateString() + "');";

            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (MySqlException)
            {
                success = false;
            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return success;
        }
        public Boolean insertErrorMissingFiles(String sender)
        {
            bool success = true;
            connection.Open();
            String statement = "INSERT INTO errores_recep (tipo_error,correo,fecha)  VALUES('No tiene campos obligatorios','" + sender + "','" + DateTime.Today.ToShortDateString() + "');";

            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (MySqlException)
            {
                success = false;
            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return success;
        }

        public Boolean insertErrorIncorrectInformation(String sender)
        {
            bool success = true;
            connection.Open();
            String statement = "INSERT INTO errores_recep (tipo_error,correo,fecha)  VALUES('Información incorrecta','" + sender + "','" + DateTime.Today.ToShortDateString() + "');";

            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (MySqlException)
            {
                success = false;
            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return success;
        }
        public Boolean insertErrorIncorrectStamp(String sender)
        {
            bool success = true;
            connection.Open();
            String statement = "INSERT INTO errores_recep (tipo_error,correo,fecha)  VALUES('Sello o certificado incorrecto','" + sender + "','" + DateTime.Today.ToShortDateString() + "');";

            try
            {
                MySqlCommand command = new MySqlCommand(statement, connection);
                command.ExecuteNonQuery();
            }
            catch (MySqlException)
            {
                success = false;
            }
            finally
            {
                connection.Dispose();
            }
            connection.Close();
            return success;
        }
    }
}
