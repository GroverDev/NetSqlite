﻿using AutoMapper;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Facturacion.Domain;
using Facturacion.Infrastructure;
using Siat.Application;

namespace Facturacion.Application;

public class LeyendasFacturaApplication
    (   ISincronizacionApplication _sincronizacionApplication,
        ILeyendasFacturaRepository _leyendasFacturaRepository,
        ISincronizacionRequestRepository _sincronizacionRequestRepository,
        IMapper _mapper
    ) : ILeyendasFacturaApplication
{
    public async Task<string> GetLeyendaFacturaAleatoria(string CodigoActividad)
    {
        string resp = await _leyendasFacturaRepository.GetLeyendaFacturaAleatoria(CodigoActividad);
        return resp;
    }

    public async Task<Response<bool>> UpdateLeyendasFactura(int createdBy)
    {
        var response = new Response<bool>();
        var sinc = await _sincronizacionRequestRepository.GetSincronizacionRequest(0);
        try
        {
            var resp = await _sincronizacionApplication.GetParametricasLeyendasFactura(sinc.CodigoPuntoVenta, sinc.CodigoSucursal, sinc.CodigoCUIS);
            if (resp.Ok)
            {
                if (await _leyendasFacturaRepository.DisableAllLeyendasFactura())
                {
                    foreach (var leyendaFacturaSiat in resp.Data)
                    {
                        var leyendaFacturaDB = await _leyendasFacturaRepository.GetLeyendaFacturaByCodigo(leyendaFacturaSiat.codigoActividad, leyendaFacturaSiat.descripcionLeyenda);
                        if (leyendaFacturaDB.CodigoActividad == "")
                        {
                            var leyendaFacturaNueva = _mapper.Map<LeyendasFactura>(leyendaFacturaSiat);
                            leyendaFacturaNueva.Id = Guid.NewGuid();
                            leyendaFacturaNueva.Created = leyendaFacturaNueva.Modified = DateTime.Now;
                            leyendaFacturaNueva.CreatedBy = leyendaFacturaNueva.ModifiedBy = createdBy;
                            leyendaFacturaNueva.State = true;
                            await _leyendasFacturaRepository.CreateLeyendaFactura(leyendaFacturaNueva);
                        } else {
                            leyendaFacturaDB.Modified = DateTime.Now;
                            leyendaFacturaDB.ModifiedBy = createdBy;
                            leyendaFacturaDB.State = true;
                            await _leyendasFacturaRepository.EnableLeyendaFactura(leyendaFacturaDB);
                        }
                    }
                    response.Ok = response.Data = true;
                } else throw new CustomException("No se pudo deshabilitar las leyendas");
            }
        }
        catch (CustomException ex) { response.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { response.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }

        return response;
    }
}
