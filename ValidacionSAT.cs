using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace RFE
{
    class ValidacionSAT
    {
        public String status;
        public String code;
        /*<summary>
         * Uses SAT's API to validate the status for the previously generated
         * invoice. The input must be on the format provided by the documentation.
        </summary>*/
        public ValidacionSAT(String chain)
        {
            
            ConsultaCFDIService.ConsultaCFDIServiceClient client = new ConsultaCFDIService.ConsultaCFDIServiceClient("BasicHttpBinding_IConsultaCFDIService");
            client.Open();
            ConsultaCFDIService.Acuse operation = new ConsultaCFDIService.Acuse();
            if (client.State == CommunicationState.Opened)
            {
                try
                {
                    operation = client.Consulta(chain);
                    status = operation.CodigoEstatus.Substring(0,7);
                    code = operation.Estado;
                }
                catch (Exception) {
                    status = "Error";
                }
            }
            client.Close();
            
        }
    }
}
