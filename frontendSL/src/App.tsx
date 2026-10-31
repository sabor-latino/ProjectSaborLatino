import { Route, Routes } from 'react-router-dom';
import { RutaProtegida } from './auth/RutaProtegida';
import { Layout } from './componentes/Layout';
import { Galeria } from './paginas/Galeria';
import { Inicio } from './paginas/Inicio';
import { Login } from './paginas/Login';
import { MiCuenta } from './paginas/MiCuenta';
import { NoEncontrada } from './paginas/NoEncontrada';
import { Paquetes } from './paginas/Paquetes';
import { PreguntasFrecuentes } from './paginas/PreguntasFrecuentes';
import { Registro } from './paginas/Registro';
import { SolicitarCotizacion } from './paginas/SolicitarCotizacion';
import { Catalogo } from './paginas/panel/Catalogo';
import { Eventos } from './paginas/panel/Eventos';
import { Panel } from './paginas/panel/Panel';
import { Resumen } from './paginas/panel/Resumen';
import { Bitacora, Configuracion } from './paginas/panel/Sistema';
import { Solicitudes } from './paginas/panel/Solicitudes';
import { Usuarios } from './paginas/panel/Usuarios';

// Todas las páginas comparten el Layout (barra de navegación y pie)
export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<Inicio />} />
        <Route path="paquetes" element={<Paquetes />} />
        <Route path="galeria" element={<Galeria />} />
        <Route path="preguntas" element={<PreguntasFrecuentes />} />
        <Route path="solicitar" element={<SolicitarCotizacion />} />
        <Route path="login" element={<Login />} />
        <Route path="registro" element={<Registro />} />
        <Route
          path="mi-cuenta"
          element={
            <RutaProtegida>
              <MiCuenta />
            </RutaProtegida>
          }
        />
        <Route
          path="panel"
          element={
            <RutaProtegida roles={['Administrador', 'Superadministrador']}>
              <Panel />
            </RutaProtegida>
          }
        >
          <Route index element={<Resumen />} />
          <Route path="solicitudes" element={<Solicitudes />} />
          <Route path="eventos" element={<Eventos />} />
          <Route path="catalogo" element={<Catalogo />} />
          <Route path="usuarios" element={<Usuarios />} />
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
