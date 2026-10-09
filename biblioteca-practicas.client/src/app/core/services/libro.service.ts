import { Injectable } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { Observable, catchError, map, tap, throwError } from 'rxjs';
import { Libro } from '../models/libro.model';


@Injectable({
  providedIn: 'root'
})
export class LibroService {
  private apiUrl = 'http://localhost:5250/api/Libros';

  constructor(private http: HttpClient) {}

  getLibros(): Observable<Libro[]> {
    return this.http.get<Libro[]>(this.apiUrl);
  }

  getTodosLosLibros(): Observable<Libro[]> {
    return this.http.get<Libro[]>(`${this.apiUrl}/todos`);
  }

  getLibro(id: number): Observable<Libro> {
    return this.http.get<Libro>(`${this.apiUrl}/${id}`);
  }

  subirLibro(formData: FormData): Observable<Libro> {
    return this.http.post<Libro>(this.apiUrl, formData);
  }

  editarLibro(id: number, datos: { titulo: string; autor?: string; descripcion?: string; categoriaId: number }): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, datos);
  }

  cambiarEstado(id: number, activo: boolean): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/estado`, activo);
  }

  descargarArchivo(ruta: string, nombreArchivo: string): void {
    const partes = ruta.split('/').filter(p => p);
    const carpeta = partes[1];
    const nombreArchivoServidor = partes[2];
    const url = `http://localhost:5250/api/archivos/${carpeta}/${nombreArchivoServidor}`;
    this.http.get(url, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const urlBlob = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = urlBlob;
        a.download = nombreArchivo || 'documento.pdf';
        a.click();
        window.URL.revokeObjectURL(urlBlob);
      },
      error: (err) => console.error('Error al descargar el archivo', err)
    });
  }
  
  registrarDescarga(id: number): Observable<number> {
    return this.http.post<number>(`${this.apiUrl}/${id}/registrar-descarga`,{});
  }

  reemplazarPdf(id: number, archivo: File): Observable<void> {
    const formData = new FormData();
    formData.append('archivo', archivo);
    return this.http.put<void>(`${this.apiUrl}/${id}/reemplazar-pdf`, formData);
  }
    private urlApiArchivo(ruta: string): string {
    const partes = ruta.split('/').filter(p => p);
    return `http://localhost:5250/api/archivos/${partes[1]}/${partes[2]}`;
  }

  verPdf(ruta: string): Observable<void> {
    const ventana = window.open('', '_blank');
    return this.http.get(this.urlApiArchivo(ruta), { responseType: 'blob' }).pipe(
      tap((blob) => {
        const url = URL.createObjectURL(blob);
        if (ventana) ventana.location.href = url;
        else window.open(url, '_blank');
        setTimeout(() => URL.revokeObjectURL(url), 60000);
      }),
      map(() => void 0),
      catchError((err) => {
        ventana?.close();
        return throwError(() => err);
      })
    );
  }

  descargarPdf(ruta: string, nombre: string): Observable<void> {
    return this.http.get(this.urlApiArchivo(ruta), { responseType: 'blob' }).pipe(
      tap((blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = nombre;
        a.click();
        URL.revokeObjectURL(url);
      }),
      map(() => void 0)
    );
  }
    /** PDF parcial, público. Devuelve el archivo y cuántas páginas muestra de cuántas tiene. */
  obtenerFragmento(ruta: string): Observable<{ blob: Blob; total: number; incluidas: number }> {
    const partes = ruta.split('/').filter(p => p);
    const url = `http://localhost:5250/api/archivos/libros/${partes[2]}/fragmento`;

    return this.http.get(url, { responseType: 'blob', observe: 'response' }).pipe(
      map((resp: HttpResponse<Blob>) => ({
        blob: resp.body as Blob,
        total: Number(resp.headers.get('X-Total-Paginas')) || 0,
        incluidas: Number(resp.headers.get('X-Paginas-Fragmento')) || 0
      }))
    );
  }
}