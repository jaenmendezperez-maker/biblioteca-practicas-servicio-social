import { Injectable } from '@angular/core';
import * as pdfjsLib from 'pdfjs-dist';
import { Libro } from '../models/libro.model';

@Injectable({
  providedIn: 'root'
})
export class ReprocesamientoService {
  private apiUrl = 'http://localhost:5250/api/Libros';

  necesitaReprocesar(libro: Libro): boolean {
    return !libro.paginasPreview?.length || !libro.tablaContenido?.length;
  }

  private headersAuth(): HeadersInit {
    const guardado = localStorage.getItem('biblioteca_auth');
    if (!guardado) return {};
    try {
      const sesion = JSON.parse(guardado);
      return sesion.token ? { Authorization: `Bearer ${sesion.token}` } : {};
    } catch {
      return {};
    }
  }

  async reprocesarLibro(libro: Libro): Promise<void> {
    const urlPdf = `http://localhost:5250${libro.rutaArchivo}`;

    const respuesta = await fetch(urlPdf);
    const arrayBuffer = await respuesta.arrayBuffer();

    const pdf = await pdfjsLib.getDocument({ data: arrayBuffer }).promise;

    const rutasPaginas: string[] = [];

    if (!libro.paginasPreview?.length) {
      const totalPaginas = Math.min(3, pdf.numPages);
      for (let i = 1; i <= totalPaginas; i++) {
        const pagina = await pdf.getPage(i);
        const viewport = pagina.getViewport({ scale: 1.2 });
        const canvas = document.createElement('canvas');
        canvas.width = viewport.width;
        canvas.height = viewport.height;
        const contexto = canvas.getContext('2d')!;
        await pagina.render({ canvasContext: contexto, viewport, canvas }).promise;

        const blob: Blob | null = await new Promise((resolve) =>
          canvas.toBlob(resolve, 'image/png')
        );

        if (blob) {
          const formData = new FormData();
          formData.append('imagen', blob, `pagina${i}.png`);
          formData.append('tipo', 'preview');

          const respuestaSubida = await fetch(
            `${this.apiUrl}/${libro.id}/subir-imagen-extraida`,
            {
              method: 'POST',
              headers: this.headersAuth(),
              body: formData
            }
          );

          if (respuestaSubida.ok) {
            const data = await respuestaSubida.json();
            rutasPaginas.push(data.ruta);
          } else {
            console.error('Error subiendo imagen', respuestaSubida.status);
          }
        }
      }
    }

    let tablaContenidoJson: string | null = null;
    if (!libro.tablaContenido?.length) {
      try {
        const outline = await (pdf as any).getOutline();
        if (outline && outline.length > 0) {
          const items = [];
          for (const item of outline.slice(0, 30)) {
            let pagina: number | null = null;
            try {
              if (item.dest) {
                const dest = typeof item.dest === 'string'
                  ? await (pdf as any).getDestination(item.dest)
                  : item.dest;
                if (dest && dest[0]) {
                  const pageIndex = await (pdf as any).getPageIndex(dest[0]);
                  pagina = pageIndex + 1;
                }
              }
            } catch {
              pagina = null;
            }
            items.push({ titulo: item.title, pagina });
          }
          tablaContenidoJson = JSON.stringify(items);
        }
      } catch (err) {
        console.error('No se pudo extraer tabla de contenido', err);
      }
    }

    const respuestaMeta = await fetch(`${this.apiUrl}/${libro.id}/metadatos-extraidos`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
        ...this.headersAuth()
      },
      body: JSON.stringify({
        rutaPortada: null,
        paginasPreview: rutasPaginas,
        tablaContenidoJson
      })
    });

    if (!respuestaMeta.ok) {
      console.error('Error actualizando metadatos', respuestaMeta.status);
    }
  }
}