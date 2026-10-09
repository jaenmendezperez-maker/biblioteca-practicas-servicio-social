import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { Libro } from '../../../core/models/libro.model';
import { Categoria } from '../../../core/models/categoria.model';
import { LibroService } from '../../../core/services/libro.service';
import { CategoriaService } from '../../../core/services/categoria.service';
import { SidebarComponent } from '../../../shared/components/sidebar/sidebar.component';
import { SidebarService } from '../../../core/services/sidebar.service';


@Component({
  selector: 'app-lista-libros',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, SidebarComponent],
  templateUrl: './lista-libros.html',
  styleUrl: './lista-libros.scss'
})
export class ListaLibrosComponent implements OnInit {
  libros: Libro[] = [];
  categorias: Categoria[] = [];
  cargando = true;
  error = '';

  textoBusqueda = '';
  categoriaSeleccionada: number | null = null;
  semestreSeleccionado: number | null = null;

  // Nuevo: controla si mostramos el hero de "categoría destacada" o no
  mostrarHeroCategorias = true;
  tituloPagina = 'Biblioteca de Prácticas';

  paginaActual = 1;
  librosPorPagina = 12;

  constructor(
    private libroService: LibroService,
    private categoriaService: CategoriaService,
    private route: ActivatedRoute,
    private router: Router,
    public sidebarService: SidebarService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.cargarLibros();
    this.categoriaService.getCategorias().subscribe({
      next: (data) => (this.categorias = data),
      error: (err) => console.error('Error cargando categorías', err)
    });

    // Si la ruta trae ?categoria=5, filtra directo por esa categoría
    this.route.queryParams.subscribe((params) => {
      const catId = params['categoria'] ? Number(params['categoria']) : null;
      const semId = params['semestre'] ? Number(params['semestre']) : null;

      this.categoriaSeleccionada = catId;
      this.semestreSeleccionado = semId;
      this.paginaActual = 1;

      // oculta el hero grande si vienes filtrado por categoría o semestre
      this.mostrarHeroCategorias = !catId && !semId;
    });

    // Si la ruta es /todos-los-libros, no mostramos el hero de categorías
    if (this.route.snapshot.data['ocultarHero']) {
      this.mostrarHeroCategorias = false;
      this.tituloPagina = 'Todos los libros';
    }
  }

  cargarLibros(): void {
    this.cargando = true;
    this.libroService.getLibros().subscribe({
      next: (data) => {
        this.libros = data;
        this.cargando = false;
      },
      error: (err) => {
        console.error(err);
        this.error = 'No se pudieron cargar los libros.';
        this.cargando = false;
      }
    });
  }

  get librosFiltrados(): Libro[] {
    return this.libros.filter((libro) => {
      const coincideTexto =
        !this.textoBusqueda ||
        libro.titulo.toLowerCase().includes(this.textoBusqueda.toLowerCase()) ||
        (libro.autor?.toLowerCase().includes(this.textoBusqueda.toLowerCase()) ?? false);

      const coincideCategoria =
        this.categoriaSeleccionada === null ||
        libro.categoriaId === this.categoriaSeleccionada;

      const coincideSemestre =
        this.semestreSeleccionado === null ||
        (libro.semestres?.some((s) => s.id === this.semestreSeleccionado) ?? false);

      return coincideTexto && coincideCategoria && coincideSemestre;
    });
  }

  get totalPaginas(): number {
    return Math.ceil(this.librosFiltrados.length / this.librosPorPagina);
  }

  get librosPaginados(): Libro[] {
    const inicio = (this.paginaActual - 1) * this.librosPorPagina;
    return this.librosFiltrados.slice(inicio, inicio + this.librosPorPagina);
  }

  get paginas(): number[] {
    return Array.from({ length: this.totalPaginas}, (_, i) => i + 1);
  }

  irAPagina(pagina: number): void {
    if (pagina < 1 || pagina > this.totalPaginas) return;
    this.paginaActual = pagina;
    window.scrollTo({ top: 0, behavior: 'smooth'});
  }

  seleccionarCategoria(id: number | null): void {
    this.categoriaSeleccionada = id;
    this.paginaActual = 1;
  }

  colorPortada(id: number): string {
    const colores = ['bg-marino-700', 'bg-ambar-600', 'bg-marino-900', 'bg-marino-400'];
    return colores[id % colores.length];
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
  
  ver(libro: Libro): void {
    if (!this.authService.estaAutenticado()) {
      this.router.navigate(['/login']);
      return;
    }
    this.libroService.verPdf(libro.rutaArchivo).subscribe({
      error: (err) => {
        if (err.status !== 401) alert('No se pudo abrir el archivo. Intenta de nuevo.');
      }
    });
  }

  descargar(libro: Libro): void {
    if (!this.authService.estaAutenticado()) {
      this.router.navigate(['/login']);
      return;
    }
    this.libroService.descargarPdf(libro.rutaArchivo, `${libro.titulo}.pdf`).subscribe({
      next: () => {
        this.libroService.registrarDescarga(libro.id).subscribe({
          next: (nuevo) => (libro.contadorDescargas = nuevo),
          error: (err) => console.error('No se pudo registrar la descarga', err)
        });
      },
      error: (err) => {
        if (err.status !== 401) alert('No se pudo descargar el archivo. Intenta de nuevo.');
      }
    });
  }
}