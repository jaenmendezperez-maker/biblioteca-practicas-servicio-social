import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Libro } from '../../../core/models/libro.model';
import { LibroService } from '../../../core/services/libro.service';
import { AuthService } from '../../../core/services/auth.service';
import { SidebarComponent } from '../../../shared/components/sidebar/sidebar.component';
import { SidebarService } from '../../../core/services/sidebar.service';
import { FavoritoService } from '../../../core/services/favorito.service';
import { AvisoLegalComponent } from '../../../shared/components/aviso-legal/aviso-legal';


@Component({
  selector: 'app-detalle-libro',
  standalone: true,
  imports: [CommonModule, RouterLink, SidebarComponent, AvisoLegalComponent],
  templateUrl: './detalle-libro.html'
})
export class DetalleLibroComponent implements OnInit {
  libro: Libro | null = null;
  cargando = true;
  error = '';
  tabActiva: 'sinopsis' | 'contenido' | 'citas' = 'sinopsis';
  mostrarVistaPrevia = false;
  librosRelacionados: Libro[] = [];
  libroNoEncontrado = false;

  private coloresPortada = [
    'bg-marino-700',
    'bg-ambar-600',
    'bg-marino-900',
    'bg-marino-400',
  ];

  esFavorito = false;
  cargandoFavorito = false;

  colorPortada(id: number): string {
    return this.coloresPortada[id % this.coloresPortada.length];
  }

  constructor(
    private route: ActivatedRoute,
    private libroService: LibroService,
    public authService: AuthService,
    public sidebarService: SidebarService,
    private favoritoService: FavoritoService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = Number(params.get('id'));
      this.cargarLibro(id);
    });
  }

  private cargarLibro(id: number): void {
    if (!id) {
      this.libroNoEncontrado = true;
      this.cargando = false;
      return;
    }

    // Resetea el estado antes de cargar el nuevo libro
    this.cargando = true;
    this.libro = null;
    this.libroNoEncontrado = false;
    this.error = '';
    this.tabActiva = 'sinopsis';

    this.libroService.getLibro(id).subscribe({
      next: (data) => {
        this.libro = data;
        this.cargando = false;
        this.verificarFavorito(data.id);
        this.cargarRelacionados(data.categoriaId, data.id);
      },
      error: (err) => {
        console.error(err);
        if (err.status === 404) {
          this.libroNoEncontrado = true;
        } else {
          this.error = 'No se pudo cargar el libro.';
        }
        this.cargando = false;
      }
    });
  }

  cambiarTab(tab: 'sinopsis' | 'contenido' | 'citas'): void {
    this.tabActiva = tab;
  }

  iniciales(titulo: string): string {
    return titulo
      .split(' ')
      .filter(w => w.length > 0)
      .slice(0, 2)
      .map(w => w[0].toUpperCase())
      .join('');
  }

  urlArchivo(ruta: string): string {
    const partes = ruta.split('/').filter(p => p);
    // partes[0] = "uploads", partes[1] = "libros"/"portadas"/"preview", partes[2] = nombreArchivo
    const carpeta = partes[1];
    const nombreArchivo = partes[2];
    return `http://localhost:5250/api/archivos/${carpeta}/${nombreArchivo}`;
  }
  
  descargarPdf(): void {
    if (!this.libro) return;

    const url = this.urlArchivo(this.libro.rutaArchivo) + '?descargar=true';
    const a = document.createElement('a');
    a.href = url;
    a.click();

    this.libroService.registrarDescarga(this.libro.id).subscribe({
      next: (nuevoContador) => {
        if (this.libro) this.libro.contadorDescargas = nuevoContador;
      },
      error: (err) => console.error('No se pudo registrar la descarga', err)
    });
  }

  abrirVistaPrevia(): void {
    this.mostrarVistaPrevia = true;
  }

  cerrarVistaPrevia(): void {
    this.mostrarVistaPrevia = false;
  }

  private verificarFavorito(libroId: number): void {
    if (!this.authService.estaAutenticado()) return;
    this.favoritoService.getFavoritos().subscribe({
      next: (favoritos) => {
        this.esFavorito = favoritos.includes(libroId);
      },
      error: (err) => console.error('Error verificando favoritos', err)
    });
  }

  private cargarRelacionados(categoriaId: number, libroActualId: number): void {
    this.libroService.getLibros().subscribe({
      next: (todos) => {
        this.librosRelacionados = todos
          .filter((l) => l.categoriaId === categoriaId && l.id !== libroActualId)
          .slice(0, 4);
      },
      error: () => {}
    });
  }

  toggleFavorito(): void {
    if (!this.libro) return;

    if (!this.authService.estaAutenticado()) {
      this.error = 'Debes iniciar sesión para guardar favoritos.';
      return;
    }

    this.cargandoFavorito = true;

    const accion = this.esFavorito
      ? this.favoritoService.quitarFavorito(this.libro.id)
      : this.favoritoService.agregarFavorito(this.libro.id);

    accion.subscribe({
      next: () => {
        this.esFavorito = !this.esFavorito;
        this.cargandoFavorito = false;
      },
      error: (err) => {
        console.error('Error actualizando favorito', err);
        this.cargandoFavorito = false;
      }
    });
  }

  formatearTamano(bytes?: number): string {
    if (!bytes) return '—';
    const mb = bytes / (1024 * 1024);
    return mb >= 1 ? `${mb.toFixed(1)} MB` : `${(bytes / 1024).toFixed(0)} KB`;
  }
}