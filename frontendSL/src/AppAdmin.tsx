import { Navigate, Route, Routes } from 'react-router-dom';
import { RutaProtegida } from './auth/RutaProtegida';
import { Layout } from './componentes/Layout';
import { Login } from './paginas/Login';
import { NoEncontrada } from './paginas/NoEncontrada';
import { Catalogo } from './paginas/panel/Catalogo';
import { Eventos } from './paginas/panel/Eventos';
import { GaleriaAdmin } from './paginas/panel/GaleriaAdmin';
import { InicioAdmin } from './paginas/panel/InicioAdmin';
import { MiPerfilAdmin } from './paginas/panel/MiPerfilAdmin';
import { Panel } from './paginas/panel/Panel';
import { PersonalAdmin } from './paginas/panel/PersonalAdmin';
import { PromocionesAdmin } from './paginas/panel/PromocionesAdmin';
import { Resumen } from './paginas/panel/Resumen';
import { Bitacora, Configuracion } from './paginas/panel/Sistema';
import { Solicitudes } from './paginas/panel/Solicitudes';
import { Usuarios } from './paginas/panel/Usuarios';

// Portal privado: login interno y consola de gestión (Administrador, Superadministrador y Trabajador).
export default function AppAdmin() {
  return (
    <Routes>
      <Route element={<Layout modoPortal="admin" />}>
        <Route index element={<Navigate to="/panel" replace />} />
        <Route path="login" element={<Login />} />
        <Route
          path="panel"
          element={
            <RutaProtegida roles={['Administrador', 'Superadministrador', 'Trabajador']}>
              <Panel />
            </RutaProtegida>
          }
        >
          <Route index element={<Resumen />} />
          <Route path="perfil" element={<MiPerfilAdmin />} />
          <Route
            path="inicio"
            element={
              <RutaProtegida roles={['Administrador', 'Superadministrador']}>
                <InicioAdmin />
              </RutaProtegida>
            }
          />
          <Route
            path="promociones"
            element={
              <RutaProtegida roles={['Administrador', 'Superadministrador']}>
                <PromocionesAdmin />
              </RutaProtegida>
            }
          />
          <Route
            path="solicitudes"
            element={
              <RutaProtegida roles={['Administrador', 'Superadministrador']}>
                <Solicitudes />
              </RutaProtegida>
            }
          />
          <Route
            path="eventos"
            element={
              <RutaProtegida roles={['Administrador', 'Superadministrador']}>
                <Eventos />
              </RutaProtegida>
            }
          />
          <Route
            path="catalogo"
            element={
              <RutaProtegida roles={['Administrador', 'Superadministrador']}>
                <Catalogo />
              </RutaProtegida>
            }
          />
          <Route
            path="personal"
            element={
              <RutaProtegida roles={['Administrador', 'Superadministrador']}>
                <PersonalAdmin />
              </RutaProtegida>
            }
          />
          <Route
            path="galeria"
            element={
              <RutaProtegida roles={['Administrador', 'Superadministrador']}>
                <GaleriaAdmin />
              </RutaProtegida>
            }
          />
          <Route
            path="usuarios"
            element={
              <RutaProtegida roles={['Administrador', 'Superadministrador']}>
                <Usuarios />
              </RutaProtegida>
            }
          />
          <Route
            path="configuracion"
            element={
              <RutaProtegida roles={['Superadministrador']}>
                <Configuracion />
              </RutaProtegida>
            }
          />
          <Route
            path="bitacora"
            element={
              <RutaProtegida roles={['Superadministrador']}>
                <Bitacora />
              </RutaProtegida>
            }
          />
        </Route>
        <Route path="*" element={<NoEncontrada />} />
      </Route>
    </Routes>
  );
}
