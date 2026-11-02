import { Route, Routes } from 'react-router-dom';
import { Layout } from './componentes/Layout';
import { Galeria } from './paginas/Galeria';
import { Inicio } from './paginas/Inicio';
import { NoEncontrada } from './paginas/NoEncontrada';
import { Paquetes } from './paginas/Paquetes';
import { PreguntasFrecuentes } from './paginas/PreguntasFrecuentes';
import { SolicitarCotizacion } from './paginas/SolicitarCotizacion';

// Vitrina pública: sin login, sin registro y sin panel. Cualquier otra ruta es 404.
export default function AppPublico() {
  return (
    <Routes>
      <Route element={<Layout modoPortal="publico" />}>
        <Route index element={<Inicio />} />
        <Route path="paquetes" element={<Paquetes />} />
        <Route path="galeria" element={<Galeria />} />
        <Route path="preguntas" element={<PreguntasFrecuentes />} />
        <Route path="solicitar" element={<SolicitarCotizacion />} />
        <Route path="*" element={<NoEncontrada />} />
      </Route>
    </Routes>
  );
}
