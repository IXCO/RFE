using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFE
{
    class Factura
    {
        public string recepientRFC;
        public string senderRFC;
        public string uuid;
        public string total;
        public string folio;
        public string error;
        
        public Factura(String RecepientRFC, String SenderRFC, String Uuid, String Total,String Folio)
        {
            recepientRFC = RecepientRFC;
            senderRFC = SenderRFC;
            uuid = Uuid;
            total = Total;
            
            folio = Folio;
        }
        public Factura()
        {
        }
        public Factura(String RecepientRFC, String SenderRFC, String Uuid)
        {
            recepientRFC = RecepientRFC;
            senderRFC = SenderRFC;
            uuid = Uuid;
            
        }
        public Factura(String errorReported)
        {
            error = errorReported;
        }
        public String getChain()
        {
            return "?re=" + senderRFC + "&rr="+recepientRFC+"&tt="+total+"&id="+uuid;
        }         
        public int statusOnSAT()
        {
            int state;
            ValidacionSAT comprobacion = new ValidacionSAT(getChain());
            switch (comprobacion.status.ToLower())
            {
               //Checks for reponse code
                case "s - com"://S Comprobante obtenido satisfactoriamente
                    if (comprobacion.code.Equals("Cancelado")) //Code is correct but status is canceled
                    {
                        state = 3;
                    }
                    else //All good
                    {
                        state = 1;
                    }
                    break;
                case "n - 601"://N 601 Comprobante incorrecto
                    state = 4;
                    break;
                case "error"://Connection error
                    state = 0;
                    break;
                default://N 603 Comprobante no encontrado
                    state = 2;
                    break;
            }
            return state;
        }
        public Boolean hasValidRFC()
        {
            ControladorBD database = new ControladorBD();
            if (database.rfcExist(recepientRFC))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
