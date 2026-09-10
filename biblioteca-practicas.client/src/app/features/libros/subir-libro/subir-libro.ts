import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { SidebarComponent } from '../../../shared/components/sidebar/sidebar.component';
import * as pdfjsLib from 'pdfjs-dist';
import { Categoria } from '../../../core/models/categoria.model';
import { CategoriaService } from '../../../core/services/categoria.service';
import { LibroService } from '../../../core/services/libro.service';
import { AuthService } from '../../../core/services/auth.service';
import { SidebarService } from '../../../core/services/sidebar.service';

pdfjsLib.GlobalWorkerOptions.workerSrc = `https://cdnjs.cloudflare.com/ajax/libs/pdf.js/${pdfjsLib.version}/pdf.worker.min.mjs`;

interface EntradaLibro {
  id: number;
  titulo: string;
  autor: string;
  descripcion: string;
  categoriaId: number | null;
  archivo: File | null;
  nombreArchivo: string;
  portadaBlob: Blob | null;
  portadaPreviewUrl: string | null;
  generandoPortada: boolean;
  error: string;
  estado: 'pendiente' | 'subiendo' | 'exito' | 'error';
  arrastrando: boolean;
  tablaContenidoJson: string | null;
}

@Component({
  selector: 'app-subir-libro',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, SidebarComponent],
  templateUrl: './subir-libro.html'
})
export class SubirLibroComponent implements OnInit {
  categorias: Categoria[] = [];
  entradas: EntradaLibro[] = [];
  private siguienteId = 1;

  enviando = false;
  errorGeneral = '';
  todoSubido = false;

  constructor(
    private categoriaService: CategoriaService,
    private libroService: LibroService,
    private authService: AuthService,
    private router: Router,
    public sidebarService: SidebarService
  ) {}

  ngOnInit(): void {
    this.categoriaService.getCategorias().subscribe({
      next: (data) => (this.categorias = data),
      error: (err) => console.error('Error cargando categorías', err)
    });

    this.agregarEntrada();
  }

  agregarEntrada(): void {
    this.entradas.push({
      id: this.siguienteId++,
      titulo: '',
      autor: '',
      descripcion: '',
      categoriaId: null,
      archivo: null,
      nombreArchivo: '',
      portadaBlob: null,
      portadaPreviewUrl: null,
      generandoPortada: false,
      error: '',
      estado: 'pendiente',
      arrastrando: false,
      tablaContenidoJson: null,
    });
  }

  quitarEntrada(id: number): void {
    if (this.entradas.length === 1) return;
    this.entradas = this.entradas.filter((e) => e.id !== id);
  }

  async onArchivoSeleccionado(event: Event, entrada: EntradaLibro): Promise<void> {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    await this.procesarArchivo(input.files[0], entrada);
  }

  async onArchivoSoltado(event: DragEvent, entrada: EntradaLibro): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    entrada.arrastrando = false;

    if (!event.dataTransfer?.files || event.dataTransfer.files.length === 0) return;

    const file = event.dataTransfer.files[0];
    await this.procesarArchivo(file, entrada);
  }

  onDragOver(event: DragEvent, entrada: EntradaLibro): void {
    event.preventDefault();
    event.stopPropagation();
    entrada.arrastrando = true;
  }

  onDragLeave(event: DragEvent, entrada: EntradaLibro): void {
    event.preventDefault();
    event.stopPropagation();
    entrada.arrastrando = false;
  }

  private async procesarArchivo(file: File, entrada: EntradaLibro): Promise<void> {
    if (file.type !== 'application/pdf') {
      entrada.error = 'Solo se permiten archivos PDF.';
      entrada.archivo = null;
      entrada.nombreArchivo = '';
      return;
    }

    if (file.size > 150 * 1024 * 1024) {
      entrada.error = 'El archivo no debe superar los 150MB.';
      entrada.archivo = null;
      entrada.nombreArchivo = '';
      return;
    }

    entrada.error = '';
    entrada.archivo = file;
    entrada.nombreArchivo = file.name;
    entrada.portadaBlob = null;
    entrada.portadaPreviewUrl = null;

    await this.generarPortada(entrada, file);
  }

  

  private async generarPortada(entrada: EntradaLibro, file: File): Promise<void> {
    entrada.generandoPortada = true;
    try {
      const arrayBuffer = await file.arrayBuffer();
      const pdf = await pdfjsLib.getDocument({ data: arrayBuffer }).promise;
      const pagina = await pdf.getPage(1);
      entrada.tablaContenidoJson = await this.extraerTablaContenido(pdf);

      const viewport = pagina.getViewport({ scale: 1.2 });
      const canvas = document.createElement('canvas');
      canvas.width = viewport.width;
      canvas.height = viewport.height;
      const contexto = canvas.getContext('2d')!;

      await pagina.render({ canvasContext: contexto, viewport, canvas }).promise;

      entrada.portadaBlob = await new Promise<Blob | null>((resolve) =>
        canvas.toBlob(resolve, 'image/png')
      );

      if (entrada.portadaBlob) {
        entrada.portadaPreviewUrl = URL.createObjectURL(entrada.portadaBlob);
      }
    } catch (err) {
      console.error('No se pudo generar la portada del PDF', err);
    } finally {
      entrada.generandoPortada = false;
    }
  }

  private async extraerTablaContenido(pdf: any): Promise<string | null> {
    try {
      const outline = await pdf.getOutline();
      if (!outline || outline.length === 0) return null;

      const items: { titulo: string; pagina: number | null }[] = [];

      for (const item of outline.slice(0, 30)) { // límite razonable
        let pagina: number | null = null;
        try {
          if (item.dest) {
            const dest = typeof item.dest === 'string'
              ? await pdf.getDestination(item.dest)
              : item.dest;
            if (dest && dest[0]) {
              const pageIndex = await pdf.getPageIndex(dest[0]);
              pagina = pageIndex + 1;
            }
          }
        } catch {
          pagina = null;
        }

        items.push({ titulo: item.title, pagina });
      }

      return JSON.stringify(items);
    } catch (err) {
      console.error('No se pudo extraer la tabla de contenido', err);
      return null;
    }
  }

  private validarEntrada(entrada: EntradaLibro): boolean {
    if (!entrada.titulo.trim()) {
      entrada.error = 'El título es obligatorio.';
      return false;
    }
    if (!entrada.categoriaId) {
      entrada.error = 'Selecciona una categoría.';
      return false;
    }
    if (!entrada.archivo) {
      entrada.error = 'Debes adjuntar un archivo PDF.';
      return false;
    }
    entrada.error = '';
    return true;
  }

  async onSubmit(): Promise<void> {
    this.errorGeneral = '';

    const usuario = this.authService.usuarioActual();
    if (!usuario) {
      this.errorGeneral = 'Debes iniciar sesión para subir libros.';
      return;
    }

    const todasValidas = this.entradas.every((e) => this.validarEntrada(e));
    if (!todasValidas) {
      this.errorGeneral = 'Revisa los libros marcados con error antes de continuar.';
      this.scrollAlPrimerError();
      return;
    }

    this.enviando = true;

    let usuarioId: string;
    try {
      const perfil = await this.authService.obtenerPerfil().toPromise();
      usuarioId = perfil!.id.toString();
    } catch (err) {
      console.error('No se pudo obtener el perfil del usuario', err);
      this.errorGeneral = 'No se pudo verificar tu sesión. Intenta iniciar sesión de nuevo.';
      this.enviando = false;
      return;
    }

    for (const entrada of this.entradas) {
      entrada.estado = 'subiendo';

      const formData = new FormData();
      if (entrada.tablaContenidoJson) {
        formData.append('TablaContenidoJson', entrada.tablaContenidoJson);
      }
      formData.append('Titulo', entrada.titulo);
      formData.append('Autor', entrada.autor);
      formData.append('Descripcion', entrada.descripcion);
      formData.append('CategoriaId', entrada.categoriaId!.toString());
      formData.append('UsuarioId', usuarioId);
      formData.append('Archivo', entrada.archivo!);
      if (entrada.portadaBlob) {
        formData.append('Portada', entrada.portadaBlob, 'portada.png');
      }

      try {
        await this.libroService.subirLibro(formData).toPromise();
        entrada.estado = 'exito';
      } catch (err) {
        console.error(err);
        entrada.estado = 'error';
        entrada.error = 'No se pudo subir este libro.';
      }
    }

    this.enviando = false;

    const todasExitosas = this.entradas.every((e) => e.estado === 'exito');
    if (todasExitosas) {
      this.todoSubido = true;
      setTimeout(() => this.router.navigate(['/']), 1800);
    } else {
      this.errorGeneral = 'Algunos libros no se pudieron subir. Revisa los detalles abajo.';
      this.scrollAlPrimerError();
    }
  }
  
  private scrollAlPrimerError(): void {
    setTimeout(() => {
      const primerErrorEl = document.querySelector('.tarjeta-con-error');
      if (primerErrorEl) {
        primerErrorEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
    }, 50);
  }

  entradasConArchivo(): number {
    return this.entradas.filter((e) => e.archivo !== null).length;
  }
  entradasSinCategoria(): number {
    return this.entradas.filter((e) => e.categoriaId === null).length;
  }
}