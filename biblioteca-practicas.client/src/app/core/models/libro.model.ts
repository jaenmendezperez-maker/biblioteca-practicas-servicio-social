import { Semestre } from "./semestre.model";

export interface Libro {
  id: number;
  titulo: string;
  autor?: string;
  descripcion?: string;
  rutaArchivo: string;
  rutaPortada?: string;
  tablaContenido?: { titulo: string; pagina: number | null }[];
  fechaSubida: string;
  categoriaId: number;
  categoriaNombre?: string;
  activo: boolean;
  paginasPreview: string;
  contadorDescargas: number;
  numeroPaginas?: number;
  tamanoBytes?: number;
  semestres?: Semestre[];
}