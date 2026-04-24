import { useState, useEffect } from 'react';

const API_BASE = 'https://wptsoftwares.giize.com/API_DGII';

function App() {
  const [semillaXml, setSemillaXml] = useState('');
  const [signedSemillaXml, setSignedSemillaXml] = useState('');
  const [tokenXml, setTokenXml] = useState('');
  const [ecfXml, setEcfXml] = useState(''); // Nuevo: Cuerpo del e-CF a enviar
  const [ecfResponse, setEcfResponse] = useState('');

  const [logs, setLogs] = useState<any[]>([]);
  const [pagination, setPagination] = useState({ current: 1, total: 1, totalItems: 0 });
  const [selectedLog, setSelectedLog] = useState<any>(null);
  const [loading, setLoading] = useState({ semilla: false, token: false, ecf: false, logs: false });
  const [status, setStatus] = useState<any>({ semilla: null, token: null, ecf: null });

  // Función para extraer el token del XML retornado
  const extractToken = (xml: string) => {
    const match = xml.match(/<token>(.*?)<\/token>/);
    return match ? match[1] : '';
  }

  const fetchLogs = async (page = 1) => {
    setLoading(prev => ({...prev, logs: true}));
    try {
      const res = await fetch(`${API_BASE}/api/admin/logs?page=${page}&pageSize=25&t=${Date.now()}`);
      if (res.ok) {
        const data = await res.json();
        setLogs(data.items || []);
        setPagination({ current: data.page, total: data.totalPages, totalItems: data.totalItems });
      }
    } catch (err) {
      console.error("Error fetching logs:", err);
    } finally {
      setLoading(prev => ({...prev, logs: false}));
    }
  };

  useEffect(() => {
    fetchLogs();
    const interval = setInterval(() => fetchLogs(pagination.current), 10000);
    return () => clearInterval(interval);
  }, [pagination.current]);

  const getSemilla = async () => {
    setLoading(prev => ({...prev, semilla: true}));
    setStatus(prev => ({...prev, semilla: null}));
    try {
      const res = await fetch(`${API_BASE}/fe/autenticacion/api/semilla`);
      if (res.ok) {
        const xml = await res.text();
        setSemillaXml(xml);
        setStatus(prev => ({...prev, semilla: 'OK'}));
      } else {
        setStatus(prev => ({...prev, semilla: 'ERROR'}));
      }
    } catch (err) {
      setStatus(prev => ({...prev, semilla: 'ERROR'}));
    } finally {
      setLoading(prev => ({...prev, semilla: false}));
      fetchLogs();
    }
  };

  const getToken = async () => {
    setLoading(prev => ({...prev, token: true}));
    setStatus(prev => ({...prev, token: null}));
    try {
      const payload = signedSemillaXml || semillaXml; 
      const res = await fetch(`${API_BASE}/fe/autenticacion/api/validacioncertificado`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/xml' },
        body: payload
      });
      if (res.ok) {
        const xml = await res.text();
        setTokenXml(xml);
        setStatus(prev => ({...prev, token: 'OK'}));
      } else {
        setStatus(prev => ({...prev, token: 'ERROR'}));
      }
    } catch (err) {
      setStatus(prev => ({...prev, token: 'ERROR'}));
    } finally {
      setLoading(prev => ({...prev, token: false}));
      fetchLogs();
    }
  };

  const enviarEcf = async () => {
    setLoading(prev => ({...prev, ecf: true}));
    setStatus(prev => ({...prev, ecf: null}));
    
    const token = extractToken(tokenXml);
    if (!token) {
      alert("No hay un Token válido. Primero realiza el Paso 2.");
      setLoading(prev => ({...prev, ecf: false}));
      return;
    }

    try {
      const res = await fetch(`${API_BASE}/fe/recepcion/api/ecf`, {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/xml',
          'Authorization': `Bearer ${token}` // Usamos el token obtenido
        },
        body: ecfXml || `<?xml version="1.0" encoding="utf-8"?><EnviOeCF>...</EnviOeCF>` 
      });
      const responseData = await res.text();
      setEcfResponse(responseData);
      if (res.ok) {
        setStatus(prev => ({...prev, ecf: 'OK'}));
      } else {
        setStatus(prev => ({...prev, ecf: 'ERROR'}));
      }
    } catch (err) {
      setStatus(prev => ({...prev, ecf: 'ERROR'}));
    } finally {
      setLoading(prev => ({...prev, ecf: false}));
      fetchLogs();
    }
  };

  return (
    <div className="container">
      <div className="header">
        <h1>WPT Manager <span>(API DGII)</span></h1>
        <p>Panel de Control y Monitorización de Comprobantes Electrónicos</p>
      </div>

      <div className="grid">
        {/* PASO 1: SEMILLA */}
        <div className="card">
          <div className="step-badge">Paso 1</div>
          <h2>Obtener Semilla</h2>
          <p>Solicita el XML de semilla inicial para la autenticación con DGII.</p>
          <button className="btn-primary" onClick={getSemilla} disabled={loading.semilla}>
            {loading.semilla ? <span className="spinner"></span> : 'Ejecutar GetSemilla'}
          </button>
          <div className={`status-box ${status.semilla}`}>
            {semillaXml ? 'Semilla recibida (XML)' : (status.semilla === 'ERROR' ? 'Error al solicitar semilla.' : 'Listo para iniciar.')}
          </div>
          {semillaXml && (
            <textarea className="code-view" readOnly value={semillaXml} style={{ height: '80px', fontSize: '10px' }} />
          )}
        </div>

        {/* PASO 2: VALIDACION & TOKEN */}
        <div className="card">
          <div className="step-badge">Paso 2</div>
          <h2>Validar & Token</h2>
          <p>Envía la semilla firmada para obtener el Token.</p>
          <textarea 
            className="code-view" 
            placeholder="Pega aquí el XML firmado..."
            value={signedSemillaXml}
            onChange={(e) => setSignedSemillaXml(e.target.value)}
            style={{ height: '60px', marginBottom: '1rem' }}
          />
          <button className="btn-accent" onClick={getToken} disabled={loading.token || (!semillaXml && !signedSemillaXml)}>
            {loading.token ? <span className="spinner"></span> : 'Obtener Token'}
          </button>
          <div className={`status-box ${status.token}`}>
            {tokenXml ? 'Token Activo ✅' : (status.token === 'ERROR' ? 'Fallo en autenticación.' : 'Esperando token...')}
          </div>
          {tokenXml && (
            <textarea className="code-view" readOnly value={tokenXml} style={{ height: '80px', fontSize: '10px', color: '#4ade80' }} />
          )}
        </div>

        {/* PASO 3: ENVÍO E-CF */}
        <div className="card">
          <div className="step-badge">Paso 3</div>
          <h2>Enviar e-CF</h2>
          <p>Pega tu XML de comprobante y envíalo usando el token activo.</p>
          <textarea 
            className="code-view" 
            placeholder="<EnviOeCF>...</EnviOeCF>"
            value={ecfXml}
            onChange={(e) => setEcfXml(e.target.value)}
            style={{ height: '100px', marginBottom: '1rem', border: '1px solid var(--success)' }}
          />
          <button className="btn-success" onClick={enviarEcf} disabled={loading.ecf || !tokenXml}>
            {loading.ecf ? <span className="spinner"></span> : 'Enviar a Recepción'}
          </button>
          <div className={`status-box ${status.ecf}`}>
            {ecfResponse ? 'Respuesta Recibida' : 'Listo para enviar comprobante.'}
          </div>
          {ecfResponse && (
            <textarea className="code-view" readOnly value={ecfResponse} style={{ height: '100px', fontSize: '10px' }} />
          )}
        </div>
      </div>

      <div className="log-section">
        <div className="log-header">
          <h2>Historial de Interacciones <span>(Monitoreo API)</span></h2>
          <button className="btn-refresh" onClick={() => fetchLogs(pagination.current)} disabled={loading.logs}>
            {loading.logs ? 'Cargando...' : 'Refrescar'}
          </button>
        </div>
        <div className="table-wrapper">
          <table className="log-table">
            <thead>
              <tr>
                <th>Fecha/Hora</th>
                <th>Servicio</th>
                <th>Método</th>
                <th>IP</th>
                <th>Estado</th>
                <th>Respuesta</th>
                <th>Latencia</th>
              </tr>
            </thead>
            <tbody>
              {logs.length === 0 ? (
                <tr><td colSpan={7} style={{ textAlign: 'center', padding: '2rem' }}>No hay logs aún.</td></tr>
              ) : (
                logs.map((log: any) => (
                  <tr key={log.logInteraccionId} onClick={() => setSelectedLog(log)} style={{ cursor: 'pointer' }}>
                    <td>{new Date(log.logInteraccionFecha).toLocaleString()}</td>
                    <td><span className="badge-service">{log.logInteraccionServicio}</span></td>
                    <td><strong>{log.logInteraccionMetodo}</strong></td>
                    <td>{log.logInteraccionIpOrigen}</td>
                    <td>
                      <span className={`status-pill ${log.logInteraccionEstado === 'OK' ? 'success' : 'error'}`}>
                        {log.logInteraccionEstado}
                      </span>
                    </td>
                    <td style={{ maxWidth: '200px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      {log.logInteraccionResponseBody}
                    </td>
                    <td>{log.logInteraccionMsRespuesta}ms</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="pagination-container">
          <div className="pagination-info">Página {pagination.current} de {pagination.total}</div>
          <div className="pagination-buttons">
            <button className="btn-page" disabled={pagination.current <= 1 || loading.logs} onClick={() => fetchLogs(pagination.current - 1)}>Anterior</button>
            <button className="btn-page" disabled={pagination.current >= pagination.total || loading.logs} onClick={() => fetchLogs(pagination.current + 1)}>Siguiente</button>
          </div>
        </div>
      </div>

      {selectedLog && (
        <div className="modal-overlay" onClick={() => setSelectedLog(null)}>
          <div className="modal-content" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Detalle de la Interacción</h3>
              <button className="btn-close" onClick={() => setSelectedLog(null)}>×</button>
            </div>
            <div className="modal-body">
              <div className="detail-grid">
                <div><strong>Servicio:</strong> {selectedLog.logInteraccionServicio}</div>
                <div><strong>Método:</strong> {selectedLog.logInteraccionMetodo}</div>
                <div><strong>IP:</strong> {selectedLog.logInteraccionIpOrigen}</div>
                <div><strong>Latencia:</strong> {selectedLog.logInteraccionMsRespuesta}ms</div>
              </div>
              <div style={{ marginTop: '1rem' }}>
                <strong>Request:</strong>
                <pre className="code-block">{selectedLog.logInteraccionRequestBody || 'Sin body'}</pre>
              </div>
              <div style={{ marginTop: '1rem' }}>
                <strong>Response:</strong>
                <pre className="code-block" style={{ borderLeftColor: 'var(--primary)' }}>{selectedLog.logInteraccionResponseBody}</pre>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default App;
